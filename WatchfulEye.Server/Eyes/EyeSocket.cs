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

    public EyeSocket(string ip, int port) {
        _connectionPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port+1);
        _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _mainSocket.Bind(_connectionPoint);

        _server = new DealerSocket($"@tcp://{ip}:{port}");
        _handler = new ZeroMQMessageHandler();
        _server.ReceiveReady += _handler.HandleMessageReceived;

        _poller = new NetMQPoller();
        _poller.Add(_server);
        _poller.RunAsync();
    }

    public void SendMessage(BaseMessage mesage) {
        _server.SendFrame(mesage.ToData());
    }

    public void StartVision() {
        RequestStreamMessage streamMessage = new RequestStreamMessage(15, _connectionPoint.Port);
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