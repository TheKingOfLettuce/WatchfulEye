namespace WatchfulEye.Shared.MessageLibrary.Messages;

public class HeartbeatMessage : BaseMessage {
    public override MessageCodes MessageCode => MessageCodes.HEARTBEAT;
}