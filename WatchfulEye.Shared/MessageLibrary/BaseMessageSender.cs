using System.Collections.Concurrent;
using NetMQ;
using NetMQ.Sockets;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Shared.Utility;

namespace WatchfulEye.Shared.MessageLibrary;

public abstract class BaseMessageSender : IDisposable {
    public event Action? OnHeartBeatPulse;

    public readonly string Name;

    protected readonly DealerSocket _socket;
    protected readonly ZeroMQMessageHandler _handler;
    protected readonly NetMQPoller _poller;
    protected readonly HeartbeatMonitor _heartBeat;
    protected readonly NetMQQueue<BaseMessage> _messageQueue;
    
    protected BaseMessageSender(string ip, int port, string name, bool isBind = true) {
        Name = name;
        _messageQueue = new NetMQQueue<BaseMessage>();
        _socket = new DealerSocket($"{(isBind ? '@' : '>')}tcp://{ip}:{port}");
        _handler = new ZeroMQMessageHandler();
        _poller = new NetMQPoller{_messageQueue};
        _heartBeat = new HeartbeatMonitor(this, _handler, 10, 10);

        _poller.Add(_socket);
        _socket.ReceiveReady += _handler.HandleMessageReceived;
        _messageQueue.ReceiveReady += HandleSendMessage;
        SubscribeMessages();
        _poller.RunAsync();
        _heartBeat.StartMonitor();
    }

    ~BaseMessageSender() {
        Dispose(false);
    }

    /// <summary>
    /// Subscribe to all messages upon construction
    /// </summary>
    protected virtual void SubscribeMessages() {
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
        Logging.Debug($"{Name} pulse checked, still alive");
        OnHeartBeatPulse?.Invoke();
    }

    public void SendMessage(BaseMessage message) {
        _messageQueue.Enqueue(message);
    }

    /// <summary>
    /// Send message, blocking until message has been sent
    /// </summary>
    /// <param name="message">the message to send</param>
    protected virtual void HandleSendMessage(object? sender, NetMQQueueEventArgs<BaseMessage> args) { 
        if (!_messageQueue.TryDequeue(out BaseMessage message, TimeSpan.FromSeconds(5))) {
            Logging.Warning("Could not dequeue message from queue");
            return;
        }

        Logging.Debug($"Attempting to send message of type {message.GetType().Name}");
        byte[] messageData = message.ToData();
        if (_socket.TrySendFrame(TimeSpan.FromSeconds(7), messageData, messageData.Length))
            Logging.Debug("Message sent sucesfully");
        else
            Logging.Warning("Failed to send message");
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