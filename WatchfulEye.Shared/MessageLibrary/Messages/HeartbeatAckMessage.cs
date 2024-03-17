namespace WatchfulEye.Shared.MessageLibrary.Messages;

public class HeartbeatAckMessage : BaseMessage {
    public override MessageCodes MessageCode => MessageCodes.HEARTBEAT_ACK;
}