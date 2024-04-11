namespace WatchfulEye.Shared.MessageLibrary.Messages.VisionRequests;

public enum VisionRequestType {
    None,
    Stream,
    Picture
}

public abstract class VisionRequestMessage : BaseMessage {
    public abstract VisionRequestType VisionRequestType {get;}
}