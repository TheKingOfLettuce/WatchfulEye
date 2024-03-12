using System.IO.Pipes;
using NetMQ;
using NetMQ.Sockets;
using WatchfulEye.Shared.MessageLibrary;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Utility;

namespace WatchfulEye.Server.Eyes;

public class EyeManager {
    private Dictionary<string, EyeSocket> _eyeSockets;
    private DealerSocket _server;
    private ZeroMQMessageHandler _handler;

    private NetMQPoller _poller;

    private static int _eyeSocketPort = 8001;

    public EyeManager() {
        _eyeSockets = new Dictionary<string, EyeSocket>();
        _server = new DealerSocket("@tcp://localhost:8000");

        _handler = new ZeroMQMessageHandler();
        _server.ReceiveReady += _handler.HandleMessageReceived;
        _handler.Subscribe<RegisterEyeMessage>(HandleRegisterEye);
        
        _poller = new NetMQPoller();
        _poller.Add(_server);
        _poller.RunAsync();
    }

    public void PostToAllSockets(BaseMessage message) {
        foreach(EyeSocket socket in _eyeSockets.Values) {
            socket.SendMessage(message);
        }
    }

    private void HandleRegisterEye(RegisterEyeMessage msg) {
        Logging.Info($"Received a Register Eye message for {msg.EyeName}");
        EyeSocket socket = new EyeSocket(msg.EyeName, port: _eyeSocketPort);
        RegisterEyeAckMessage msgAck = new RegisterEyeAckMessage(_eyeSocketPort, msg.EyeName);
        _eyeSocketPort += 2;
        _eyeSockets.Add(msg.EyeName, socket);
        Logging.Debug("Eye socket created, sending register ack back");
        _server.SendFrame(msgAck.ToData());
    }
}