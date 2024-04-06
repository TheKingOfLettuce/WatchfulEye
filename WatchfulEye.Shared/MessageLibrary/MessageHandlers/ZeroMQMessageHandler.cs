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
                Publish(MessageFactory.DeserializeMsg<RegisterEyeMessage>(msgData.Item2));
                break;
            case MessageCodes.REGISTER_EYE_ACK:
                Publish(MessageFactory.DeserializeMsg<RegisterEyeAckMessage>(msgData.Item2));
                break;
            case MessageCodes.REQUEST_STREAM:
                Publish(MessageFactory.DeserializeMsg<RequestStreamMessage>(msgData.Item2));
                break;
            case MessageCodes.HEARTBEAT:
                Publish(MessageFactory.DeserializeMsg<HeartbeatMessage>(msgData.Item2));
                break;
            case MessageCodes.HEARTBEAT_ACK:
                Publish(MessageFactory.DeserializeMsg<HeartbeatAckMessage>(msgData.Item2));
                break;
            case MessageCodes.DEREGISTER_EYE:
                Publish(MessageFactory.DeserializeMsg<DeRegisterEyeMessage>(msgData.Item2));
                break;
        }

        return;
    }
}