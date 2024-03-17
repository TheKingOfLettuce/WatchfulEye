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

namespace WatchfulEye.Server.Eyes;

public class EyeSocket : IDisposable {
    private readonly IPEndPoint _connectionPoint;
    private readonly Socket _mainSocket;

    private DealerSocket _server;
    private ZeroMQMessageHandler _handler;
    private NetMQPoller _poller;
    private string _eyeName;
    private AutoResetEvent _heartbeatAck;

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
        _heartbeatAck = new AutoResetEvent(false);
        Task.Run(HearbeatLoop);
    }

    private void SubscribeMessages() {
        _server.ReceiveReady += _handler.HandleMessageReceived;

        _handler.Subscribe<HeartbeatMessage>(HandleHeartbeatMessage);
        _handler.Subscribe<HeartbeatAckMessage>(HandleHeartbeatAck);
    }

    public void SendMessage(BaseMessage mesage) {
        byte[] messageData = mesage.ToData();
        _server.SendFrame(messageData, messageData.Length);
    }

    private async Task HearbeatLoop() {
        await Task.Delay(7500);
        Logging.Debug("Starting heartbeat loop");
        byte[] heartbeatData = new HeartbeatMessage().ToData();
        while (true) {
            Logging.Debug("Attempting to send heartbeat message");
            if (!_server.TrySendFrame(TimeSpan.FromSeconds(7.5), heartbeatData, heartbeatData.Length)) {
                Logging.Error("Could not send heart beat message to eye");
                EyeManager.HandleDeregisterEye(new DeRegisterEyeMessage(_eyeName));
                return;
            }
            _heartbeatAck.Reset();
            if (_heartbeatAck.WaitOne(TimeSpan.FromSeconds(5))) {
                Logging.Debug("Heartbeat acknowledged, waiting another 30 seconds");
                await Task.Delay(30000);
            }
            else {
                Logging.Error($"Did not receive heart beat ack within 5 seconds");
                EyeManager.HandleDeregisterEye(new DeRegisterEyeMessage(_eyeName));
                return;
            }
        }
    }

    private void HandleHeartbeatMessage(HeartbeatMessage message) {
        Logging.Debug("Received heartbeat message from eyeball, sending ack");
        SendMessage(new HeartbeatAckMessage());
    }

    private void HandleHeartbeatAck(HeartbeatAckMessage message) {
        Logging.Debug("Received heartbeat ack message from eyeball");
        _heartbeatAck.Set();
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
    }
}