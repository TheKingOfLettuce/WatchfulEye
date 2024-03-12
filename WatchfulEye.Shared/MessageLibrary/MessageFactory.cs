using System.Text;
using System.Text.Json;
using NetMQ;
using WatchfulEye.Shared.MessageLibrary.Messages;

namespace WatchfulEye.Shared.MessageLibrary;

public static class MessageFactory {
    public static (MessageCodes, string) GetMessageData(byte[] data) {
        if (data.Length == 0) {
            return default;
        }
        MessageCodes msgCode = (MessageCodes)data[0];
        string jsonString = Encoding.UTF8.GetString(data, 1, data.Length-1);
        return (msgCode, jsonString);
    }

    public static T DeserializeMsg<T>(string jsonData) where T : BaseMessage {
        return JsonSerializer.Deserialize<T>(jsonData);
    }
}