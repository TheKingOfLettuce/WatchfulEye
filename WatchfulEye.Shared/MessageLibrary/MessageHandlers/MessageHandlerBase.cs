using System;
using System.Collections.Generic;
using WatchfulEye.Shared.MessageLibrary.Messages;

namespace WatchfulEye.Shared.MessageLibrary.MessageHandlers;

public class MessageHandlerBase {
    private Dictionary<Type, CallbackHandlerBase> _services;

    public MessageHandlerBase() {
        _services = new Dictionary<Type, CallbackHandlerBase>();
    }

    public void Subscribe<T>(Action<T> func) where T : BaseMessage {
        Type messageType = typeof(T);
        if (!_services.ContainsKey(messageType)) {
            _services.Add(messageType, new CallbackHandler<T>());
        }

        (_services[messageType] as CallbackHandler<T>).AddCallback(func);
    }

    public void Unsubscribe<T>(Action<T> func) where T : BaseMessage {
        Type messageType = typeof(T);
        if (!_services.ContainsKey(messageType)) return;

        (_services[messageType] as CallbackHandler<T>).RemoveCallback(func);
    }

    public void Publish<T>(T message) where T : BaseMessage {
        Type messageType = message.GetType();
        if (!_services.ContainsKey(messageType)) return;

        (_services[messageType] as CallbackHandler<T>).HandleMessage(message);
    }
}