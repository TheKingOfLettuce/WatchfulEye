using System.Net.Sockets;
using WatchfulEye.Shared.MessageLibrary;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Utility;

namespace WatchfulEye.Server.Eyes;

public class EyeManager : IDisposable {
    private Dictionary<string, EyeSocket> _eyeSockets;

    private static int _eyeSocketPort = 8001;
    private CancellationTokenSource _networkDiscoverCancel;

    public EyeManager() {
        _eyeSockets = new Dictionary<string, EyeSocket>();
        
        _networkDiscoverCancel = new CancellationTokenSource();
        Task.Run(NetworkDiscovery, _networkDiscoverCancel.Token);
    }

    public async Task NetworkDiscovery() {
        const int DiscoverPort = 8888;
        Logging.Debug($"Beginning network discovery on port {DiscoverPort}");
        UdpClient server = new UdpClient(DiscoverPort);

        while (true) {
            Logging.Debug("Waiting for Registration message in NetworkDiscovery");
            // wait for client message
            UdpReceiveResult clientResults = await server.ReceiveAsync();

            Logging.Debug("Receieved data during NetoworkDisocery");
            // decode register message
            (MessageCodes, string) msgData = MessageFactory.GetMessageData(clientResults.Buffer);
            if (msgData.Item1 != MessageCodes.REGISTER_EYE) {
                Logging.Warning($"Network discovery received a message that wasn't a register message");
                continue;
            }

            // handle register message
            string localIP = IPUtils.GetLocalIP();
            HandleRegisterEye(MessageFactory.DeserializeMsg<RegisterEyeMessage>(msgData.Item2), localIP, _eyeSocketPort);
            Logging.Debug("Eye socket created, sending register ack back");

            // send ack message back
            byte[] msgAckData = new RegisterEyeAckMessage(_eyeSocketPort, localIP).ToData();
            await server.SendAsync(msgAckData, msgAckData.Length, clientResults.RemoteEndPoint);
            _eyeSocketPort += 2;
            Logging.Debug("Registration Acknowledgment sent");
        }
    }

    public void PostToAllSockets(BaseMessage message) {
        foreach(EyeSocket socket in _eyeSockets.Values) {
            socket.SendMessage(message);
        }
    }

    public void ViewAllVision() {
        foreach(EyeSocket socket in _eyeSockets.Values) {
            socket.StartVision();
        }
    }

    private void HandleRegisterEye(RegisterEyeMessage msg, string ip, int port) {
        Logging.Info($"Received a Register Eye message for {msg.EyeName}");
        EyeSocket socket = new EyeSocket(ip, port: port);
        _eyeSockets.Add(msg.EyeName, socket);
    }

    public void Dispose() {
        Logging.Debug($"Disposing {nameof(EyeManager)}");
        GC.SuppressFinalize(this);

        _networkDiscoverCancel.Cancel();
        foreach(EyeSocket eye in _eyeSockets.Values) {
            eye.Dispose();
        }
    }
}