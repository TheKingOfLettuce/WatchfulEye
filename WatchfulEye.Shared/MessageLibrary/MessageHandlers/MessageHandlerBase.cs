using WatchfulEye.Shared.MessageLibrary.Messages;

namespace WatchfulEye.Shared.MessageLibrary.MessageHandlers;

#pragma warning disable CS8602 // null reference, but we can gurantee the callbacks are not null

/// <summary>
/// Base class for having a collection of message handlers
/// </summary>
public class MessageHandlerBase {
    private Dictionary<Type, CallbackHandlerBase> _services;

    public MessageHandlerBase() {
        _services = new Dictionary<Type, CallbackHandlerBase>();
    }

    /// <summary>
    /// Subscribes a method to given <see cref="BaseMessage"/>
    /// </summary>
    /// <param name="func">the method to callback to</param>
    /// <typeparam name="T">the message to fire on</typeparam>
    public void Subscribe<T>(Action<T> func) where T : BaseMessage {
        Type messageType = typeof(T);
        if (!_services.ContainsKey(messageType)) {
            _services.Add(messageType, new CallbackHandler<T>());
        }

        (_services[messageType] as CallbackHandler<T>).AddCallback(func);
    }

    /// <summary>
    /// Unsubscribes a method to given <see cref="BaseMessage"/>
    /// </summary>
    /// <param name="func">the method to remove on</param>
    /// <typeparam name="T">the message to fire on</typeparam>
    public void Unsubscribe<T>(Action<T> func) where T : BaseMessage {
        Type messageType = typeof(T);
        if (!_services.ContainsKey(messageType)) return;

        (_services[messageType] as CallbackHandler<T>).RemoveCallback(func);
    }

    /// <summary>
    /// Publishes a <see cref="BaseMessage"/> and fires any callbacks that our subscribed
    /// </summary>
    /// <param name="message">the message to publish</param>
    /// <typeparam name="T">the type of message publishing</typeparam>
    public void Publish<T>(T message) where T : BaseMessage {
        Type messageType = message.GetType();
        if (!_services.ContainsKey(messageType)) return;

        (_services[messageType] as CallbackHandler<T>).HandleMessage(message);
    }
}

#pragma warning restore CS8602