using System.Text.Json.Serialization;
using LettuceTalk.Core;

namespace WatchfulEye.Shared.MessageLibrary.Messages.VisionRequests;

[MessageData(MessageCodes.REQUEST_STREAM)]
public class RequestStreamMessage : VisionRequestMessage {
    public override VisionRequestType VisionRequestType => VisionRequestType.Stream;

    [JsonInclude]
    public readonly float StreamLength;
    [JsonInclude]
    public readonly int Port;
    [JsonInclude]
    public readonly int VideoWidth;
    [JsonInclude]
    public readonly int VideoHeight;
    [JsonInclude]
    public readonly int Framerate;


    [JsonConstructor]
    public RequestStreamMessage(float streamLength, int port, int videoWidth = 640, int videoHeight = 480, int framerate = 24) {
        StreamLength = streamLength;
        VideoWidth = videoWidth;
        VideoHeight = videoHeight;
        Framerate = framerate;
        Port = port;
    }
}