using NetMQ;
using NetMQ.Sockets;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages;

namespace WatchfulEye.Shared.MessageLibrary;

public abstract class BaseMessageSender : IDisposable {
    public event Action? OnHeartBeatPulse;

    protected readonly DealerSocket _socket;
    protected readonly ZeroMQMessageHandler _handler;
    protected readonly NetMQPoller _poller;
    protected readonly HeartbeatMonitor _heartBeat;
    
    protected BaseMessageSender(string ip, int port, bool isBind = true) {
        _socket = new DealerSocket($"{(isBind ? '@' : '>')}tcp://{ip}:{port}");
        _handler = new ZeroMQMessageHandler();
        _poller = new NetMQPoller();
        _heartBeat = new HeartbeatMonitor(this, _handler, 10, 10);

        _poller.Add(_socket);
        _poller.RunAsync();
        SubscribeMessages();
        _heartBeat.StartMonitor();
    }

    ~BaseMessageSender() {
        Dispose(false);
    }

    /// <summary>
    /// Subscribe to all messages upon construction
    /// </summary>
    protected virtual void SubscribeMessages() {
        _socket.ReceiveReady += _handler.HandleMessageReceived;
        _heartBeat.OnHeartBeatFail += OnHeartBeatFail;
        _heartBeat.OnHeartBeat += OnHeartBeat;
    }

    /// <summary>
    /// Handler method for when our heart beat fails
    /// </summary>
    protected abstract void OnHeartBeatFail();

    /// <summary>
    /// Handler method for when our heat beat "beats"
    /// </summary>
    protected virtual void OnHeartBeat() {
        OnHeartBeatPulse?.Invoke();
    }

    /// <summary>
    /// Send message, blocking until message has been sent
    /// </summary>
    /// <param name="message">the message to send</param>
    public virtual void SendMessage(BaseMessage message) {
        byte[] messageData = message.ToData();
        _socket.SendFrame(messageData, messageData.Length);
    }

    /// <summary>
    /// Send message, attempts to send until timeout has passed
    /// </summary>
    /// <param name="message">the message to send</param>
    /// <param name="timeOut">how long to attempt to send message, in seconds</param>
    /// <returns>if it sent the message or not within the time</returns>
    public virtual bool SendMessage(BaseMessage message, float timeOut) {
        return SendMessage(message, TimeSpan.FromSeconds(timeOut));
    }

    /// <summary>
    /// Send message, attempts to send until timeout has passed
    /// </summary>
    /// <param name="message">the message to send</param>
    /// <param name="timeOut">how long to attempt to send message</param>
    /// <returns>if it sent the message or not within the time</returns>
    public virtual bool SendMessage(BaseMessage message, TimeSpan timeOut) {
        byte[] messageData = message.ToData();
        return _socket.TrySendFrame(timeOut, messageData, messageData.Length);
    }

    public virtual void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Internal dispose method to handle freeing or closing any resources
    /// </summary>
    /// <param name="fromDispose">if it was called from <see cref="Dispose"/></param>
    protected virtual void Dispose(bool fromDispose) {
        if (!fromDispose) return;

        _poller.Stop();
        _poller.Dispose();
        _socket.Dispose();
        _heartBeat.Dispose();
    }
}