using NetMQ;
using WatchfulEye.Shared.MessageLibrary.Messages;

namespace WatchfulEye.Shared.MessageLibrary.MessageHandlers;

public class ZeroMQMessageHandler : MessageHandlerBase {
    public void HandleMessageReceived(object? sender, NetMQSocketEventArgs args) {
        byte[] data = args.Socket.ReceiveFrameBytes();
        HandleMessage(data);
    }

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
        }

        return;
    }
}