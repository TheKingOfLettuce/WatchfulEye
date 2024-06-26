using System.Text.Json.Serialization;

namespace WatchfulEye.Shared.MessageLibrary.Messages;

public class RegisterEyeAckMessage : BaseMessage {
    public override MessageCodes MessageCode => MessageCodes.REGISTER_EYE_ACK;

    [JsonInclude]
    public readonly int Port;
    [JsonInclude]
    public readonly string ServerIP;

    [JsonConstructor]
    public RegisterEyeAckMessage(int port, string serverIp) {
        Port = port;
        ServerIP = serverIp;
    }
}