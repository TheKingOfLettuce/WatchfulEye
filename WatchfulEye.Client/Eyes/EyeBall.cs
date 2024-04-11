using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NetMQ;
using NetMQ.Sockets;
using WatchfulEye.Shared;
using WatchfulEye.Shared.MessageLibrary;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Shared.MessageLibrary.Messages.VisionRequests;
using WatchfulEye.Utility;

namespace WatchfulEye.Client.Eyes;

/// <summary>
/// The eyes of our world
/// </summary>
public class EyeBall : IDisposable {
    public readonly AutoResetEvent DisconnectedWaiter;
    public readonly string EyeName;
    public readonly string SocketIP;

    private DealerSocket _client;
    private ZeroMQMessageHandler _handler;
    private NetMQPoller _poller;
    private bool _isBusy;

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

    /// <summary>
    /// Helper method to subscribe to all the messages we care about
    /// </summary>
    private void SubscribeMessages() {
        _client.ReceiveReady += _handler.HandleMessageReceived;

        _handler.Subscribe<RequestStreamMessage>(HandleStreamRequest);
        _handler.Subscribe<RequestPictureMessage>(HandlePictureRequest);
    }

    /// <summary>
    /// Sends a message to the connecting eye ball client
    /// </summary>
    /// <param name="message">the message to send</param>
    public void SendMessage(BaseMessage message) {
        byte[] messageData = message.ToData();
        _client.SendFrame(messageData, messageData.Length);
    }

    /// <summary>
    /// Method for when heartbeat fails
    /// </summary>
    private void OnHeartBeatFail() {
        Logging.Fatal($"Heartbeat Failure");
        DisconnectedWaiter.Set();
    }

    /// <summary>
    /// Method for when heart beat "beats"
    /// </summary>
    private void OnHeartBeat() {
        Logging.Debug("Received heartbeat from EyeSocket");
    }

    #region Picture

    private void HandlePictureRequest(RequestPictureMessage message) {
        Logging.Info("Received Picture Request");
        if (_isBusy) {
            Logging.Warning("Cannot take a picture, vision is currently busy");
            return;
        }
        Task.Run(() => TakePicture(message));
    }

    private async Task TakePicture(RequestPictureMessage message) {
        Logging.Debug("Starting up picture python process");
        if (SocketIP == null) {
            Logging.Error("Picture has been requested but current Socket IP is null");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = "python",
            ArgumentList = {
                Path.Combine(Directory.GetCurrentDirectory(), "PythonScripts", "TakePicture.py"),
                message.PictureWidth.ToString(),
                message.PictureHeight.ToString(),
                SocketIP,
                message.Port.ToString()
            },
            CreateNoWindow = false
        };

        await StartVisionProcess(startInfo, message);
    }

    #endregion

    #region Stream
    /// <summary>
    /// Starts the video stream process when we receive a <see cref="RequestStreamMessage"/>
    /// </summary>
    /// <param name="message">the <see cref="RequestStreamMessage"/></param>
    private void HandleStreamRequest(RequestStreamMessage message) {
        Logging.Info($"Got Stream Request");
        if (_isBusy) {
            Logging.Warning("Cannot stream video, vision is currently busy");
            return;
        }
        Task.Run(() => StreamVideo(message));
    }

    /// <summary>
    /// Starts the python script that streams the PI camera video
    /// </summary>
    /// <param name="message">the <see cref="RequestStreamMessage"/> that has init data</param>
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

        await StartVisionProcess(startInfo, message);
    }
    #endregion

    private async Task<bool> StartVisionProcess(ProcessStartInfo startInfo, VisionRequestMessage message) {
        Logging.Debug("Starting python process");
        using Process? pythonStream = Process.Start(startInfo);
        if (pythonStream == null) {
            Logging.Error("Failed to start python process");
            return false;
        }

        _isBusy = true;
        SendMessage(new VisionReadyMessage(message.VisionRequestType));
        await pythonStream.WaitForExitAsync();
        Logging.Debug("Python process finshed");
        _isBusy = false;
        return true;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);

        _poller.Stop();
        _poller.Dispose();

        _client.Dispose();
        _heartBeat.Dispose();
    }

    /// <summary>
    /// Static method to get an <see cref="EyeBall"/> by attempting to socket with eye manager
    /// </summary>
    /// <param name="eyeName">the eye name to create with</param>
    /// <returns>the <see cref="EyeBall"/> it socketed with, or null if failed to socket</returns>
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
        RegisterEyeAckMessage? ackMessage = MessageFactory.DeserializeMsg<RegisterEyeAckMessage>(receiveMsg.Item2);
        if (ackMessage == default) {
            Logging.Error("Received an Ack message that failed JSON parse");
            throw new Exception("Failed to parse JSON Ack message");
        }

        // fully socket
        client.Close();
        return new EyeBall(ackMessage.ServerIP, ackMessage.Port, eyeName);
    }
}