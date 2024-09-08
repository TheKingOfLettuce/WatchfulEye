using System.Text.Json.Serialization;
using LettuceTalk.Core;

namespace WatchfulEye.Shared.MessageLibrary.Messages.General;

[MessageData(MessageCodes.DEREGISTER_EYE)]
public class DeRegisterEyeMessage : Message {
    [JsonInclude]
    public readonly string EyeName;

    [JsonConstructor]
    public DeRegisterEyeMessage(string eyeName) {
        EyeName = eyeName;
    }
}