using System.Text.Json.Serialization;

namespace WatchfulEye.Shared.MessageLibrary.Messages;

public class RequestStreamMessage : BaseMessage
{
    public override MessageCodes MessageCode => MessageCodes.REQUEST_STREAM;

    [JsonInclude]
    public readonly float StreamLength;

    [JsonConstructor]
    public RequestStreamMessage(float streamLength) {
        StreamLength = streamLength;
    }
}