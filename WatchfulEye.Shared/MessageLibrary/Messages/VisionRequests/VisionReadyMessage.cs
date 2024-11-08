using System.Text.Json.Serialization;
using LettuceTalk.Core;

namespace WatchfulEye.Shared.MessageLibrary.Messages.VisionRequests;

[MessageData]
public class VisionReadyMessage : Message {
    [JsonInclude]
    public readonly VisionRequestType RequestType;

    [JsonConstructor]
    public VisionReadyMessage(VisionRequestType requestType) {
        RequestType = requestType;
    }
}