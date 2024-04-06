using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using Serilog;
using WatchfulEye.Utility;
using WatchfulEye.Shared.MessageLibrary;
using NetMQ.Sockets;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using NetMQ;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Shared;

namespace WatchfulEye.Server.Eyes;

public class EyeSocket : IDisposable {
    private readonly IPEndPoint _connectionPoint;
    private readonly Socket _mainSocket;
    private readonly HeartbeatMonitor _heartBeat;

    private DealerSocket _server;
    private ZeroMQMessageHandler _handler;
    private NetMQPoller _poller;
    private string _eyeName;

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

    private void SubscribeMessages() {
        _server.ReceiveReady += _handler.HandleMessageReceived;
    }

    public void SendMessage(BaseMessage mesage) {
        byte[] messageData = mesage.ToData();
        _server.SendFrame(messageData, messageData.Length);
    }

    private void OnHeartBeatFail() {
        Logging.Error($"Heartbeat Failure");
        EyeManager.HandleDeregisterEye(new DeRegisterEyeMessage(_eyeName));
    }

    private void OnHeartBeat() {
        Logging.Debug("Heartbeat from EyeBall");
    }

    public void StartVision() {
        RequestStreamMessage streamMessage = new RequestStreamMessage(15, _connectionPoint.Port, 1280, 720);
        Listen();
        SendMessage(streamMessage);
        Task.Run(() => VLCLauncer.ConnectToVision(this, streamMessage.StreamLength+5));
    }


    public void Listen() {
        _mainSocket.Listen();
        Logging.Debug($"EyeSocket is listening for vision");
    }

    public async Task<Stream?> GetDataStreamAsync() {
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