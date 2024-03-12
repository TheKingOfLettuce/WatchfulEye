using System.Text.Json.Serialization;

namespace WatchfulEye.Shared.MessageLibrary.Messages;

public class RegisterEyeMessage : BaseMessage {
    public override MessageCodes MessageCode => MessageCodes.REGISTER_EYE;

    [JsonInclude]
    public readonly string EyeName;

    [JsonConstructor]
    public RegisterEyeMessage(string eyeName) {
        EyeName = eyeName;
    }
}