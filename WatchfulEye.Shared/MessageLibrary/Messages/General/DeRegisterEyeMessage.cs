using System.Text.Json.Serialization;

namespace WatchfulEye.Shared.MessageLibrary.Messages.General;

public class DeRegisterEyeMessage : BaseMessage {
    public override MessageCodes MessageCode => MessageCodes.DEREGISTER_EYE;

    [JsonInclude]
    public readonly string EyeName;

    [JsonConstructor]
    public DeRegisterEyeMessage(string eyeName) {
        EyeName = eyeName;
    }
}