using System.Net.Sockets;
using WatchfulEye.Shared.MessageLibrary;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Utility;

namespace WatchfulEye.Server.Eyes;

/// <summary>
/// Static manager for EyeSockets, handling discovery and registration
/// </summary>
public static class EyeManager {
    public static IReadOnlyCollection<EyeSocket> EyeSockets => _eyeSockets.Values;
    private static Dictionary<string, EyeSocket> _eyeSockets;

    private static int _eyeSocketPort = 8001;
    private static CancellationTokenSource _networkDiscoverCancel;
    private static bool _enabled;

    static EyeManager() {
        _eyeSockets = new Dictionary<string, EyeSocket>();
        
        _networkDiscoverCancel = new CancellationTokenSource();
    }

    /// <summary>
    /// Starts the network discovery loop in a separate thread
    /// </summary>.
    public static void StartNetworkDiscovery() {
        if (_enabled) return;

        _enabled = true;
        CancellationToken token = _networkDiscoverCancel.Token;
        Task.Run(() => NetworkDiscovery(token), token);
    }

    /// <summary>
    /// Stops the network discovery loop by cancelling
    /// </summary>
    public static void StopNetworkDiscovery() {
        if (!_enabled) return;

        _enabled = false;
        _networkDiscoverCancel.Cancel();
    }

    /// <summary>
    /// Deregisters an eye from the given <paramref name="eyeName"/>
    /// </summary>
    /// <param name="eyeName">the name of the eye to remove</param>
    public static void DeregisterEye(string eyeName) => HandleDeregisterEye(new DeRegisterEyeMessage(eyeName));

    /// <summary>
    /// Post a message to all registered <see cref="EyeSocket"/>
    /// </summary>
    /// <param name="message">the message to post</param>
    public static void PostToAllSockets(BaseMessage message) {
        foreach(EyeSocket socket in _eyeSockets.Values) {
            socket.SendMessage(message);
        }
    }

    /// <summary>
    /// Start the vision viewing process on all registered <see cref="EyeSocket"/>
    /// </summary>
    /// <seealso cref="EyeSocket.StartVision"/>
    public static void ViewAllVision() {
        foreach(EyeSocket socket in _eyeSockets.Values) {
            socket.StartVision();
        }
    }

    public static void GetLatestThumbnails() {
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Thumbnails")))
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Thumbnails"));

        foreach(EyeSocket socket in _eyeSockets.Values) {
            socket.RequestPicture();
        }
    }

    /// <summary>
    /// Network discovery loop, waits for a registration message and sends an acknowledgement to fully socket eye
    /// </summary>
    /// <param name="token">the token to cancel our loop</param>
    private static async Task NetworkDiscovery(CancellationToken token) {
        const int DiscoverPort = 8888;
        Logging.Debug($"Beginning network discovery on port {DiscoverPort}");
        UdpClient server = new UdpClient(DiscoverPort);

        while (!token.IsCancellationRequested) {
            Logging.Debug("Waiting for Registration message in NetworkDiscovery");
            // wait for client message
            UdpReceiveResult clientResults;
            try {
                clientResults = await server.ReceiveAsync(token);
            }
            catch (OperationCanceledException) {
                break;
            }

            Logging.Debug("Receieved data during NetoworkDisocery");
            // decode register message
            (MessageCodes, string) msgData = MessageFactory.GetMessageData(clientResults.Buffer);
            if (msgData.Item1 != MessageCodes.REGISTER_EYE) {
                Logging.Warning($"Network discovery received a message that wasn't a register message");
                continue;
            }

            // handle register message
            string localIP = IPUtils.GetLocalIP();
            if (!HandleRegisterEye(MessageFactory.DeserializeMsg<RegisterEyeMessage>(msgData.Item2), localIP, _eyeSocketPort)) {
                Logging.Warning("Didn't register eye, not sending ack");
                continue;
            }
            Logging.Debug("Eye socket created, sending register ack back");

            // send ack message back
            byte[] msgAckData = new RegisterEyeAckMessage(_eyeSocketPort, localIP).ToData();
            await server.SendAsync(msgAckData, msgAckData.Length, clientResults.RemoteEndPoint);
            _eyeSocketPort += 2;
            Logging.Debug("Registration Acknowledgment sent");
        }

        Logging.Debug("Network discovery has stopped its loop");
    }

    /// <summary>
    /// Handless receiving a register eye message
    /// </summary>
    /// <param name="msg">the <see cref="RegisterEyeMessage"/></param>
    /// <param name="ip">the local ip for socket</param>
    /// <param name="port">the port to bind to for socket</param>
    /// <returns>if it successfully registered</returns>
    private static bool HandleRegisterEye(RegisterEyeMessage msg, string ip, int port) {
        Logging.Info($"Received a Register Eye message for {msg.EyeName}");
        if (_eyeSockets.ContainsKey(msg.EyeName)) {
            Logging.Warning($"Already have eye socket named {msg.EyeName}");
            return false;
        }
        EyeSocket socket = new EyeSocket(ip, port, msg.EyeName);
        _eyeSockets.Add(msg.EyeName, socket);
        return true;
    }

    /// <summary>
    /// Handles receiving a de-register eye message
    /// </summary>
    /// <param name="message">the <see cref="DeRegisterEyeMessage"/></param>
    private static void HandleDeregisterEye(DeRegisterEyeMessage message) {
        Logging.Info($"Received DeRegister Eye Message for {message.EyeName}");
        if (!_eyeSockets.ContainsKey(message.EyeName)) {
            Logging.Warning($"No eye socket with name {message.EyeName}");
            return;
        }

        EyeSocket eye = _eyeSockets[message.EyeName];
        _eyeSockets.Remove(message.EyeName);
        eye.Dispose();
    }

    /// <summary>
    /// Manual implementation of Dispose for static class, clears sockets and stops network discovery
    /// </summary>
    public static void Dispose() {
        Logging.Debug($"Disposing {nameof(EyeManager)}");

        StopNetworkDiscovery();
        foreach(EyeSocket eye in _eyeSockets.Values) {
            eye.Dispose();
        }
    }
}