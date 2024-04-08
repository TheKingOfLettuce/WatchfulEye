using NetMQ;
using WatchfulEye.Shared.MessageLibrary.Messages;

namespace WatchfulEye.Shared.MessageLibrary.MessageHandlers;

public class ZeroMQMessageHandler : MessageHandlerBase {

    /// <summary>
    /// Callback method for ZeroMQ sockets when data is received
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public void HandleMessageReceived(object? sender, NetMQSocketEventArgs args) {
        byte[] data = args.Socket.ReceiveFrameBytes();
        HandleMessage(data);
    }

    /// <summary>
    /// Handle converting the raw message data into a <see cref="BaseMessage"/> to publish
    /// </summary>
    /// <param name="data">the raw message data</param>
    private void HandleMessage(byte[] data) {
        (MessageCodes, string) msgData = MessageFactory.GetMessageData(data);
        switch(msgData.Item1) {
            case MessageCodes.REGISTER_EYE:
                AttemptPublish<RegisterEyeMessage>(msgData.Item2);
                break;
            case MessageCodes.REGISTER_EYE_ACK:
                AttemptPublish<RegisterEyeAckMessage>(msgData.Item2);
                break;
            case MessageCodes.REQUEST_STREAM:
                AttemptPublish<RequestStreamMessage>(msgData.Item2);
                break;
            case MessageCodes.HEARTBEAT:
                AttemptPublish<HeartbeatMessage>(msgData.Item2);
                break;
            case MessageCodes.HEARTBEAT_ACK:
                AttemptPublish<HeartbeatAckMessage>(msgData.Item2);
                break;
            case MessageCodes.DEREGISTER_EYE:
                AttemptPublish<DeRegisterEyeMessage>(msgData.Item2);
                break;
            case MessageCodes.REQUEST_PICTURE:
                AttemptPublish<RequestPictureMessage>(msgData.Item2);
                break;
        }
    }

    private void AttemptPublish<T>(string jsonMsg) where T : BaseMessage {
        T? message = MessageFactory.DeserializeMsg<T>(jsonMsg);
        if (message == default)
            throw new Exception($"Failed to convert JSON data for message type {nameof(T)}");
        Publish(message);
    }
}