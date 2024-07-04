namespace WatchfulEye.Shared.MessageLibrary.Messages.General;

public class HeartbeatMessage : BaseMessage {
    public override MessageCodes MessageCode => MessageCodes.HEARTBEAT;
}