using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using NetMQ;
using NetMQ.Sockets;
using WatchfulEye.Shared.MessageLibrary;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Utility;

namespace WatchfulEye.Server.Eyes;

public class EyeManager {
    private Dictionary<string, EyeSocket> _eyeSockets;

    private static int _eyeSocketPort = 8001;

    public EyeManager() {
        _eyeSockets = new Dictionary<string, EyeSocket>();
        
        Task.Run(NetworkDiscovery);
    }

    public async Task NetworkDiscovery() {
        const int DiscoverPort = 8888;
        Logging.Debug($"Beginning network discovery on port {DiscoverPort}");
        UdpClient server = new UdpClient(DiscoverPort);

        while (true) {
            IPEndPoint serverIP = new IPEndPoint(IPAddress.Any, 0);
            byte[] receiveData = server.Receive(ref serverIP);
            (MessageCodes, string) msgData = MessageFactory.GetMessageData(receiveData);
            if (msgData.Item1 != MessageCodes.REGISTER_EYE) {
                Logging.Warning($"Network discovery received a message that wasn't a register message");
                continue;
            }
            HandleRegisterEye(MessageFactory.DeserializeMsg<RegisterEyeMessage>(msgData.Item2), IPUtils.GetLocalIP(), _eyeSocketPort);
            Logging.Debug("Eye socket created, sending register ack back");
            byte[] msgAckData = new RegisterEyeAckMessage(_eyeSocketPort, IPUtils.GetLocalIP()).ToData();
            await server.SendAsync(msgAckData, msgAckData.Length, serverIP);
            _eyeSocketPort += 2;
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
        _eyeSockets.Add(ip, socket);
    }
}