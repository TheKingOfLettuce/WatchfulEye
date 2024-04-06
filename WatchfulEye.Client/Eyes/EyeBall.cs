using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using WatchfulEye.Shared;
using WatchfulEye.Shared.MessageLibrary;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Utility;

namespace WatchfulEye.Client.Eyes;

public class EyeBall : IDisposable {
    public readonly AutoResetEvent DisconnectedWaiter;
    public readonly string EyeName;
    public readonly string SocketIP;

    private DealerSocket _client;
    private ZeroMQMessageHandler _handler;
    private NetMQPoller _poller;

    private readonly HeartbeatMonitor _heartBeat;
    
    public EyeBall(string ip, int port, string eyeName) {
        EyeName = eyeName;
        SocketIP = ip;

        _client = new DealerSocket($"tcp://{ip}:{port}");
        _handler = new ZeroMQMessageHandler();
        _poller = new NetMQPoller();
        SubscribeMessages();
        _poller.Add(_client);
        _poller.RunAsync();

        DisconnectedWaiter = new AutoResetEvent(false);

        _heartBeat = new HeartbeatMonitor(_client, _handler, 10, 10);
        _heartBeat.OnHeartBeatFail += OnHeartBeatFail;
        _heartBeat.OnHeartBeat += OnHeartBeat;
        _heartBeat.StartMonitor();
        
        Logging.Info($"New eye ball created {EyeName}");
    }

    private void SubscribeMessages() {
        _client.ReceiveReady += _handler.HandleMessageReceived;

        _handler.Subscribe<RequestStreamMessage>(HandleStreamRequest);
    }

    private void OnHeartBeatFail() {
        Logging.Fatal($"Heartbeat Failure");
        DisconnectedWaiter.Set();
    }

    private void OnHeartBeat() {
        Logging.Debug("Received heartbeat from EyeSocket");
    }

    #region Stream
    private void HandleStreamRequest(RequestStreamMessage message) {
        Logging.Info($"Got Stream Request: {message.StreamLength}");
        Task.Run(() => StreamVideo(message));
    }

    private async Task StreamVideo(RequestStreamMessage message) {
        Logging.Debug($"Starting up a video stream python process");
        if (SocketIP == null) {
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
                SocketIP,
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
        _heartBeat.Dispose();
    }

    public static EyeBall? SocketEye(string eyeName) {
        using UdpClient client = new UdpClient();
        client.EnableBroadcast = true;
        // 10 second receive timeout
        client.Client.ReceiveTimeout = 10000;
        Logging.Debug("Attempting to socket eye");

        byte[] receiveData;
        const int Retry_Count = 10;
        int numRetries = 0;
        while (true) {
            try {
                // send register
                Logging.Info("Sending Registration for EyeBall");
                byte[] msgData = new RegisterEyeMessage(eyeName).ToData();
                client.Send(new RegisterEyeMessage(eyeName).ToData(), msgData.Length, new IPEndPoint(IPAddress.Broadcast, 8888));

                // attempt receive ack
                Logging.Info("Waiting for Registration Acknowledgement");
                IPEndPoint serverIP = new IPEndPoint(IPAddress.Any, 0);
                receiveData = client.Receive(ref serverIP);
                Logging.Debug("Received message from network discover");
                break;
            }
            catch (SocketException e) {
                Logging.Error($"Socket exception with code [{e.SocketErrorCode}]", e);
                if (e.SocketErrorCode != SocketError.TimedOut) {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }
            }

            if (numRetries == Retry_Count) {
                Logging.Fatal($"Registration exceeded retries [{Retry_Count}], not trying again");
                client.Close();
                return null;
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
        client.Close();
        return new EyeBall(ackMessage.ServerIP, ackMessage.Port, eyeName);
    }
}