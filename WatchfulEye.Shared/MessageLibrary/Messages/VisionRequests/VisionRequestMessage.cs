using LettuceTalk.Core;

namespace WatchfulEye.Shared.MessageLibrary.Messages.VisionRequests;

public enum VisionRequestType {
    None,
    Stream,
    Picture
}


public abstract class VisionRequestMessage : Message {
    public abstract VisionRequestType VisionRequestType {get;}
}