using System.Text;
using System.Text.Json;
using NetMQ;
using WatchfulEye.Shared.MessageLibrary.Messages;

namespace WatchfulEye.Shared.MessageLibrary;

/// <summary>
/// Factory to create messages from raw data
/// </summary>
public static class MessageFactory {
    /// <summary>
    /// Return Message MetaData from raw data, giving its <see cref="MessageCodes"/> and json data
    /// </summary>
    /// <param name="data">the raw data to pull</param>
    /// <returns>a tuple of (<see cref="MessageCodes"/>, string JSON data)</returns>
    public static (MessageCodes, string) GetMessageData(byte[] data) {
        if (data.Length == 0) {
            return default;
        }
        MessageCodes msgCode = (MessageCodes)data[0];
        string jsonString = Encoding.UTF8.GetString(data, 1, data.Length-1);
        return (msgCode, jsonString);
    }

    /// <summary>
    /// Helper method to convert string JSON data to the given message
    /// </summary>
    /// <param name="jsonData">the string JSON data</param>
    /// <typeparam name="T">the <see cref="BaseMessage"/> type to convert to</typeparam>
    /// <returns>the converted message, or null if it couldn't convert</returns>
    public static T? DeserializeMsg<T>(string jsonData) where T : BaseMessage {
        return JsonSerializer.Deserialize<T>(jsonData);
    }
}