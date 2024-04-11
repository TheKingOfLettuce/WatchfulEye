using System.Text.Json.Serialization;

namespace WatchfulEye.Shared.MessageLibrary.Messages.VisionRequests;

public class VisionReadyMessage : BaseMessage {
    public override MessageCodes MessageCode => MessageCodes.VISION_READY;

    [JsonInclude]
    public readonly VisionRequestType RequestType;

    [JsonConstructor]
    public VisionReadyMessage(VisionRequestType requestType) {
        RequestType = requestType;
    }
}