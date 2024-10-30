using System.Net.Sockets;
using WatchfulEye.Shared.MessageLibrary;
using LettuceTalk.Core;
using WatchfulEye.Shared.MessageLibrary.Messages.General;
using WatchfulEye.Shared.Utility;
using LettuceTalk.NetMQ;

namespace WatchfulEye.Server.Eyes;

/// <summary>
/// Static manager for EyeSockets, handling discovery and registration
/// </summary>
public static class EyeManager {
    public static readonly NetMQServer Server;

    public static event Action<EyeSocket>? OnEyeSocketAdded;
    public static event Action<EyeSocket>? OnEyeSocketRemoved;

    private static readonly string _serverIP;
    private const int _serverPort = 8000;
    private static int _eyeSocketPort = 8001;
    private static CancellationTokenSource _networkDiscoverCancel;
    private static bool _enabled;

    static EyeManager() {
        _serverIP = IPUtils.GetLocalIP();
        Server = new NetMQServer(_serverIP, _serverPort);
        
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

            // handle register message
            RegisterEyeMessage register = (RegisterEyeMessage)MessageFactory.GetMessage(clientResults.Buffer);
            if (register == default) {
                Logging.Error("Failed to parse JSON register message");
                continue;
            }
            if (!HandleRegisterEye(register, _eyeSocketPort)) {
                Logging.Error("Didn't register eye, not sending ack");
                continue;
            }
            Logging.Debug("Eye socket created, sending register ack back");

            // send ack message back
            byte[] msgAckData = MessageFactory.GetMessageData(new RegisterEyeAckMessage(_serverPort, _serverIP));
            await server.SendAsync(msgAckData, msgAckData.Length, clientResults.RemoteEndPoint);
            _eyeSocketPort++;
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
    private static bool HandleRegisterEye(RegisterEyeMessage msg, int port) {
        Logging.Info($"Received a Register Eye message for {msg.EyeName}");
        EyeSocket socket;
        try {
            socket = new EyeSocket(port, msg.EyeName);
            Server.PreRegisterClient(msg.EyeName, socket, false);
        }
        catch (Exception e) {
            Logging.Error("Failed to pre-register client to server", e);
            return false;
        }
        
        OnEyeSocketAdded?.Invoke(socket);
        return true;
    }

    /// <summary>
    /// Handles receiving a de-register eye message
    /// </summary>
    /// <param name="message">the <see cref="DeRegisterEyeMessage"/></param>
    private static void HandleDeregisterEye(DeRegisterEyeMessage message) {
        Logging.Info($"Received DeRegister Eye Message for {message.EyeName}");
        EyeSocket removedSocket;
        try {
            removedSocket = (EyeSocket)Server.GetClientCallbackHandler(message.EyeName);
            Server.DeregisterClient(message.EyeName);
        }
        catch (Exception e) {
            Logging.Error($"Failed to remove Eye Socket with name {message.EyeName}", e);
            return;
        }

        OnEyeSocketRemoved?.Invoke(removedSocket);
        removedSocket.Dispose(false);
    }

    /// <summary>
    /// Manual implementation of Dispose for static class, clears sockets and stops network discovery
    /// </summary>
    public static void Dispose() {
        Logging.Debug($"Disposing {nameof(EyeManager)}");

        StopNetworkDiscovery();
        Server.Dispose();
    }
}