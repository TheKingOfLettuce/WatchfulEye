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

    public EyeSocket(string ip = "localhost", int port = 8000) {
        _connectionPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port+1);
        _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _mainSocket.Bind(_connectionPoint);

        _server = new DealerSocket($"@tcp://localhost:{port}");
        _handler = new ZeroMQMessageHandler();
        _server.ReceiveReady += _handler.HandleMessageReceived;

        _poller = new NetMQPoller();
        _poller.Add(_server);
        _poller.RunAsync();
    }

    public void SendMessage(BaseMessage mesage) {
        _server.SendFrame(mesage.ToData());
    }


    public void Listen() {
        _mainSocket.Listen();
        Logging.Debug($"EyeSocket listening");
    }

    public async Task<Stream> GetDataStreamAsync() {
        Logging.Debug($"Looking to accept a connection");
        Socket handle = await _mainSocket.AcceptAsync();
        Logging.Debug($"Accepted an Eye Connection, creating network stream");
        return new NetworkStream(handle, true);
    }

    public void Dispose() {
        GC.SuppressFinalize(this);

        _mainSocket.Shutdown(SocketShutdown.Both);
        _mainSocket.Close();
        _mainSocket.Dispose();

        _poller.Stop();
        _poller.Dispose();

        _server.Dispose();
    }
}