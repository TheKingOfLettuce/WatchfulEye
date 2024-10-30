using System.Net;
using System.Net.Sockets;
using Serilog;
using WatchfulEye.Shared.Utility;
using WatchfulEye.Shared.MessageLibrary.Messages.VisionRequests;
using LettuceTalk.NetMQ;
using LettuceTalk.Core.MessageHandlers;
using LettuceTalk.Core;

namespace WatchfulEye.Server.Eyes;

/// <summary>
/// The "Socket" for the EyeBalls out in the world
/// </summary>
public class EyeSocket : MessageCallbackHandler {
    public readonly string EyeName;
    public event Action<VisionRequestType>? OnVisionReady;

    private readonly IPEndPoint _connectionPoint;
    private readonly Socket _mainSocket;

    /// <summary>
    /// Starts our EyeSocket at the given connection point
    /// </summary>
    /// <param name="ip">the ip address of the connection</param>
    /// <param name="port">the port of the connection</param>
    /// <param name="eyeName">the name of the eyeball</param>
    public EyeSocket(int port, string eyeName) : base() {
        EyeName = eyeName;
        _connectionPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port);
        _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _mainSocket.Bind(_connectionPoint);
        _mainSocket.Listen();
        SubscribeMessages();
    }


    protected void SubscribeMessages() {
        Subscribe<VisionReadyMessage>(HandleVisionReady);
    }

    /// <summary>
    /// Request a video stream vision to the client
    /// </summary>
    public void RequestStream() {
        RequestStreamMessage streamMessage = new RequestStreamMessage(15, _connectionPoint.Port, 1280, 720);
        SendMessageToClient(streamMessage);
    }

    /// <summary>
    /// Request a picture vision to the client
    /// </summary>
    /// <param name="width">the width of the picture</param>
    /// <param name="height">the height of the picture</param>
    public void RequestPicture(int width, int height) {
        RequestPictureMessage request = new RequestPictureMessage(_connectionPoint.Port, width, height);
        SendMessageToClient(request);
    }

    protected void SendMessageToClient(Message message) {
        EyeManager.Server.SendMessage(new SendClientMessageArgs(EyeName, message));
    }

    /// <summary>
    /// Callback for when client lets us know there is a vision connection ready
    /// </summary>
    /// <param name="message"></param>
    private void HandleVisionReady(VisionReadyMessage message) {
        OnVisionReady?.Invoke(message.RequestType);
    }

    /// <summary>
    /// Handler method for when our heart beat fails
    /// </summary>
    protected void OnHeartBeatFail() {
        Logging.Error($"Heartbeat Failure");
        EyeManager.DeregisterEye(EyeName);
    }

    /// <summary>
    /// Gets the data stream of our vision on the client
    /// </summary>
    /// <returns>the stream of vision data</returns>
    public async Task<Stream?> GetNetworkStreamAsync() {
        const float Time_Seconds = 10;
        Logging.Debug($"Looking to accept a connection within {Time_Seconds} seconds");
        using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Time_Seconds));
        try {
            Socket handle = await _mainSocket.AcceptAsync(timeout.Token);
            Logging.Debug($"Accepted an Eye Connection, creating network stream");
            return new NetworkStream(handle, true);
        }
        catch (OperationCanceledException canceledE) {
            Log.Warning("Timeout occurred when attempting vision connection", canceledE);
            return null;
        }
    }

    /// <summary>
    /// Disposes our socket, closing IPCs and heartbeats
    /// </summary>
    public void Dispose(bool fromDispose) {
        Logging.Debug($"Disposing {nameof(EyeSocket)}");
        if (!fromDispose) return;

        _mainSocket.Close();
        _mainSocket.Dispose();
    }
}