using System.Text.Json.Serialization;
using LettuceTalk.Core;

namespace WatchfulEye.Shared.MessageLibrary.Messages.General;

[MessageData(MessageCodes.REGISTER_EYE)]
public class RegisterEyeMessage : Message {
    [JsonInclude]
    public readonly string EyeName;

    [JsonConstructor]
    public RegisterEyeMessage(string eyeName) {
        EyeName = eyeName;
    }
}