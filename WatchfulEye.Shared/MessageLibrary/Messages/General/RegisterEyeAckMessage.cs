using System.Text.Json.Serialization;
using LettuceTalk.Core;

namespace WatchfulEye.Shared.MessageLibrary.Messages.General;

[MessageData(MessageCodes.REGISTER_EYE_ACK)]
public class RegisterEyeAckMessage : Message {
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