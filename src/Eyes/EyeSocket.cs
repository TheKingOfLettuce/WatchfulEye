using System.Net;
using System.Net.Sockets;
using Serilog;
using WatchfulEye.Utility;

namespace WatchfulEye.Eyes;

public class EyeSocket : IDisposable {
    private readonly IPEndPoint _connectionPoint;
    private readonly Socket _mainSocket;

    public EyeSocket(string ip = "0.0.0.0", int port = 8000) {
        _connectionPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _mainSocket.Bind(_connectionPoint);
        _mainSocket.Listen();
        Logging.Debug($"New EyeSocket listening at {ip}:{port}");
    }

    public async Task<Stream> GetDataStreamAsync() {
        Logging.Debug($"Looking to accept a connection");
        Socket handle = await _mainSocket.AcceptAsync();
        Logging.Debug($"Accepted an Eye Connection, creating network stream");
        return new NetworkStream(handle, true);
    }

    public async Task StreamData() {
        Logging.Debug($"Looking to accept a connection");
        Socket handle = await _mainSocket.AcceptAsync();
        Logging.Debug($"Accepted an Eye Connection");
        Logging.Debug("Beggining data stream");
        while (true) {
            byte[] data = new byte[1024];
            int receivedBytes = await handle.ReceiveAsync(data, SocketFlags.None);
            if (receivedBytes <= 0) {
                Log.Debug($"Received {receivedBytes} stopping stream");
                break;
            }
        }

        Logging.Debug("Closing listening socket");
        handle.Shutdown(SocketShutdown.Both);
        handle.Close();
        handle.Dispose();
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        _mainSocket.Shutdown(SocketShutdown.Both);
        _mainSocket.Close();
        _mainSocket.Dispose();
    }
}