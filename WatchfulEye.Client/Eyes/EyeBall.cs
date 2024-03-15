using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NetMQ;
using NetMQ.Sockets;
using WatchfulEye.Shared.MessageLibrary;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Utility;

namespace WatchfulEye.Client.Eyes;

public class EyeBall : IDisposable {
    public readonly string EyeName;

    private DealerSocket _client;
    private ZeroMQMessageHandler _handler;
    private NetMQPoller _poller;

    private string? _socketIP;
    
    public EyeBall(string eyeName) {
        EyeName = eyeName;
        _client = new DealerSocket();
        _handler = new ZeroMQMessageHandler();
        _poller = new NetMQPoller();
        SubscribeMessages();
        _poller.Add(_client);
        _poller.RunAsync();
        Logging.Info($"New eye ball created {EyeName}");
    }

    private void SubscribeMessages() {
        _client.ReceiveReady += _handler.HandleMessageReceived;

        _handler.Subscribe<RequestStreamMessage>(HandleStreamRequest);
    }

    public void SocketEye() {
        UdpClient client = new UdpClient();
        client.EnableBroadcast = true;
        Logging.Debug("Attempting to socket eye");

        // send register
        byte[] msgData = new RegisterEyeMessage(EyeName).ToData();
        client.Send(new RegisterEyeMessage(EyeName).ToData(), msgData.Length, new IPEndPoint(IPAddress.Broadcast, 8888));

        // attempt receive ack
        IPEndPoint serverIP = new IPEndPoint(IPAddress.Any, 0);
        byte[] receiveData = client.Receive(ref serverIP);
        Logging.Debug("Received message from network discover");

        // decode ack
        (MessageCodes, string) receiveMsg = MessageFactory.GetMessageData(receiveData);
        if (receiveMsg.Item1 != MessageCodes.REGISTER_EYE_ACK) {
            Logging.Error("Received a message back that is not a register ack message, cannot proceed");
            throw new Exception("Failed to parse or receive ACK message");
        }
        RegisterEyeAckMessage ackMessage = MessageFactory.DeserializeMsg<RegisterEyeAckMessage>(receiveMsg.Item2);

        // fully socket
        HandleRegisterEyeAck(ackMessage);
        client.Close();
        
    }

    private void HandleRegisterEyeAck(RegisterEyeAckMessage msg) {
        Logging.Debug($"Received ack back from Network discover {msg.ServerIP} Port {msg.Port}");
        Logging.Debug("Connecting to eye socket");
        _client.Connect($"tcp://{msg.ServerIP}:{msg.Port}");
        Logging.Info("Connected to eye socket");
        _socketIP = msg.ServerIP;
    }

    private void HandleStreamRequest(RequestStreamMessage message) {
        Logging.Info($"Got Stream Request: {message.StreamLength}");
        Task.Run(() => StreamVideo(message));
    }

    private async Task StreamVideo(RequestStreamMessage message) {
        Logging.Debug($"Starting up a video stream python process");
        if (_socketIP == null) {
            Logging.Error("Stream has been requested but current Socket IP is null");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = "python",
            ArgumentList = {
                Path.Combine(Directory.GetCurrentDirectory(), "PythonScripts", "StreamVideo.py"),
                message.VideoWidth.ToString(),
                message.VideoHeight.ToString(),
                message.Framerate.ToString(),
                _socketIP,
                message.Port.ToString(),
                message.StreamLength.ToString()
            },
            CreateNoWindow = false
        };

        Logging.Debug("Starting python stream");
        using Process? pythonStream = Process.Start(startInfo);
        if (pythonStream == null) {
            Logging.Error("Failed to start python stream");
            return;
        }
        await pythonStream.WaitForExitAsync();
        Logging.Debug("Python stream finished");
    }

    public void Dispose() {
        GC.SuppressFinalize(this);

        _poller.Stop();
        _poller.Dispose();

        _client.Dispose();
    }
}