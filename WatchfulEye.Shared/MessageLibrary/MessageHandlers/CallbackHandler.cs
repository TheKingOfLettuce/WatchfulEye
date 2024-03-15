using WatchfulEye.Shared.MessageLibrary.Messages;

namespace WatchfulEye.Shared.MessageLibrary.MessageHandlers;

public interface CallbackHandlerBase {}

public class CallbackHandler<T> : CallbackHandlerBase where T : BaseMessage  {
    private event Action<T>? _callbacks;

    public void AddCallback(Action<T> func) {
        _callbacks += func;
    }

    public void RemoveCallback(Action<T> func) {
        _callbacks -= func;
    }

    public void HandleMessage(T message) {
        _callbacks?.Invoke(message);
    }
}