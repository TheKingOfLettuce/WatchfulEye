using System.Net;
using System.Net.Sockets;
using Serilog;
using WatchfulEye.Utility;
using NetMQ.Sockets;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using NetMQ;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Shared;

namespace WatchfulEye.Server.Eyes;

/// <summary>
/// The "Socket" for the EyeBalls out in the world
/// </summary>
public class EyeSocket : IDisposable {
    private readonly IPEndPoint _connectionPoint;
    private readonly Socket _mainSocket;
    private readonly HeartbeatMonitor _heartBeat;

    private DealerSocket _server;
    private ZeroMQMessageHandler _handler;
    private NetMQPoller _poller;
    private string _eyeName;

    /// <summary>
    /// Starts our EyeSocket at the given connection point
    /// </summary>
    /// <param name="ip">the ip address of the connection</param>
    /// <param name="port">the port of the connection</param>
    /// <param name="eyeName">the name of the eyeball</param>
    public EyeSocket(string ip, int port, string eyeName) {
        _connectionPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port+1);
        _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _mainSocket.Bind(_connectionPoint);

        _server = new DealerSocket($"@tcp://{ip}:{port}");
        _handler = new ZeroMQMessageHandler();
        _poller = new NetMQPoller();

        SubscribeMessages();

        _poller.Add(_server);
        _poller.RunAsync();
        _eyeName = eyeName;

        _heartBeat = new HeartbeatMonitor(_server, _handler, 10, 10);
        _heartBeat.OnHeartBeatFail += OnHeartBeatFail;
        _heartBeat.OnHeartBeat += OnHeartBeat;
        _heartBeat.StartMonitor();
    }

    /// <summary>
    /// A helper method to subscribe to all the messages we care about
    /// </summary>
    private void SubscribeMessages() {
        _server.ReceiveReady += _handler.HandleMessageReceived;
    }

    /// <summary>
    /// Sends a message to the connecting eye ball client
    /// </summary>
    /// <param name="mesage">the message to send</param>
    public void SendMessage(BaseMessage mesage) {
        byte[] messageData = mesage.ToData();
        _server.SendFrame(messageData, messageData.Length);
    }

    /// <summary>
    /// Starts the vision process by sending a <see cref="RequestStreamMessage"/> to the client
    /// and then passing ourselves to VLC for stream viewing
    /// </summary>
    public void StartVision() {
        RequestStreamMessage streamMessage = new RequestStreamMessage(15, _connectionPoint.Port, 1280, 720);
        Listen();
        SendMessage(streamMessage);
        Task.Run(() => VLCLauncer.ConnectToVision(this, streamMessage.StreamLength+5));
    }

    public void RequestPicture() {
        RequestPictureMessage request = new RequestPictureMessage(_connectionPoint.Port, 1280, 720);
        Listen();
        Task.Run(SaveCurrentView);
        SendMessage(request);
    }

    private async void SaveCurrentView() {
        using Stream? stream = await GetNetworkStreamAsync();
        if (stream == null) {
            Logging.Error("Failed to get network stream");
            return;
        }

        using var fileStream = File.Create(Path.Combine(Directory.GetCurrentDirectory(),"Thumbnails", _eyeName+".jpg"));
        await stream.CopyToAsync(fileStream);
    }

    /// <summary>
    /// Handler method for when our heart beat fails
    /// </summary>
    private void OnHeartBeatFail() {
        Logging.Error($"Heartbeat Failure");
        EyeManager.DeregisterEye(_eyeName);
    }

    /// <summary>
    /// Handler method for when our heat beat "beats"
    /// </summary>
    private void OnHeartBeat() {
        Logging.Debug($"Heartbeat from EyeBall {_eyeName}");
    }

    /// <summary>
    /// Starts listening on the port we defined for vision access on the client
    /// </summary>
    private void Listen() {
        _mainSocket.Listen();
        Logging.Debug($"EyeSocket is listening for vision");
    }

    /// <summary>
    /// Gets the data stream of our vision on the client
    /// </summary>
    /// <returns>the stream of vision data</returns>
    public async Task<Stream?> GetNetworkStreamAsync() {
        const int Time_Seconds = 10000;
        Logging.Debug($"Looking to accept a connection within {Time_Seconds/1000} seconds");
        CancellationTokenSource timeout = new CancellationTokenSource(Time_Seconds);
        try {
            Socket handle = await _mainSocket.AcceptAsync(timeout.Token);
            Logging.Debug($"Accepted an Eye Connection, creating network stream");
            return new NetworkStream(handle, true);
        }
        catch (OperationCanceledException canceledE) {
            Log.Warning("Timeout occured when attempting vision connection", canceledE);
            return null;
        }
    }

    /// <summary>
    /// Disposes our socket, closing IPCs and heartbeats
    /// </summary>
    public void Dispose() {
        Logging.Debug($"Disposing {nameof(EyeSocket)}");
        GC.SuppressFinalize(this);

        _mainSocket.Close();
        _mainSocket.Dispose();

        _poller.Stop();
        _poller.Dispose();

        _server.Dispose();
        _heartBeat.Dispose();
    }
}