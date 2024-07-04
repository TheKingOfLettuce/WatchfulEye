namespace WatchfulEye.Shared.MessageLibrary.Messages.General;

public class HeartbeatAckMessage : BaseMessage {
    public override MessageCodes MessageCode => MessageCodes.HEARTBEAT_ACK;
}