using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using WatchfulEye.Shared.MessageLibrary;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Utility;

namespace WatchfulEye.Client.Eyes;

public class EyeBall : IDisposable {
    public readonly AutoResetEvent DisconnectedWaiter;
    public readonly string EyeName;

    private DealerSocket _client;
    private ZeroMQMessageHandler _handler;
    private NetMQPoller _poller;

    private string? _socketIP;
    private AutoResetEvent _heartbeatAck;
    
    public EyeBall(string eyeName) {
        EyeName = eyeName;
        _client = new DealerSocket();
        _handler = new ZeroMQMessageHandler();
        _poller = new NetMQPoller();
        SubscribeMessages();
        _poller.Add(_client);
        _poller.RunAsync();
        Logging.Info($"New eye ball created {EyeName}");
        _heartbeatAck = new AutoResetEvent(false);
        DisconnectedWaiter = new AutoResetEvent(false);
    }

    private void SubscribeMessages() {
        _client.ReceiveReady += _handler.HandleMessageReceived;

        _handler.Subscribe<RequestStreamMessage>(HandleStreamRequest);
        _handler.Subscribe<HeartbeatAckMessage>(HandleHeartbeatAck);
        _handler.Subscribe<HeartbeatMessage>(HandleHeartbeat);
    }

    public void SocketEye() {
        using UdpClient client = new UdpClient();
        client.EnableBroadcast = true;
        // 10 second receive timeout
        client.Client.ReceiveTimeout = 10000;
        Logging.Debug("Attempting to socket eye");

        byte[] receiveData = null;
        const int Retry_Count = 10;
        int numRetries = 0;
        while (true) {
            try {
                // send register
                Logging.Info("Sending Registration for EyeBall");
                byte[] msgData = new RegisterEyeMessage(EyeName).ToData();
                client.Send(new RegisterEyeMessage(EyeName).ToData(), msgData.Length, new IPEndPoint(IPAddress.Broadcast, 8888));

                // attempt receive ack
                Logging.Info("Waiting for Registration Acknowledgement");
                IPEndPoint serverIP = new IPEndPoint(IPAddress.Any, 0);
                receiveData = client.Receive(ref serverIP);
                Logging.Debug("Received message from network discover");
                break;
            }
            catch (SocketException e) {
                Logging.Error($"Socket exception with code [{e.ErrorCode}]", e);
            }

            if (numRetries == Retry_Count) {
                Logging.Fatal($"Registration exceeded retries [{Retry_Count}], not trying again");
                client.Close();
                DisconnectedWaiter.Set();
                return;
            }

            Logging.Warning($"Registration attempt #{numRetries} failed, attempting again");
            numRetries++;
        }
        
        

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
        Task.Run(HearbeatLoop);
        
    }

    private void HandleRegisterEyeAck(RegisterEyeAckMessage msg) {
        Logging.Debug($"Received ack back from Network discover {msg.ServerIP} Port {msg.Port}");
        Logging.Debug("Connecting to eye socket");
        _client.Connect($"tcp://{msg.ServerIP}:{msg.Port}");
        Logging.Info("Connected to eye socket");
        _socketIP = msg.ServerIP;
    }

    private async Task HearbeatLoop() {
        Logging.Debug("Starting heartbeat loop");
        byte[] heartbeatData = new HeartbeatMessage().ToData();
        while (true) {
            Logging.Debug("Sending heartbeat message");
            _client.SendFrame(heartbeatData, heartbeatData.Length);
            _heartbeatAck.Reset();
            if (_heartbeatAck.WaitOne(TimeSpan.FromSeconds(5))) {
                Logging.Debug("Heartbeat acknowledged, waiting another 30 seconds");
                await Task.Delay(30000);
            }
            else {
                Logging.Fatal($"Did not receive heart beat ack within 5 seconds");
                DisconnectedWaiter.Set();
                return;
            }
        }
    }

    private void HandleHeartbeat(HeartbeatMessage message) {
        Logging.Debug("Eyeball received heartbeat, sending ack");
        byte[] data = new HeartbeatAckMessage().ToData();
        _client.SendFrame(data, data.Length);
    }

    private void HandleHeartbeatAck(HeartbeatAckMessage message) {
        Logging.Debug("Received heart beat acknowledgement");
        _heartbeatAck.Set();
    }

    #region Stream
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
    #endregion

    public void Dispose() {
        GC.SuppressFinalize(this);

        _poller.Stop();
        _poller.Dispose();

        _client.Dispose();
    }
}