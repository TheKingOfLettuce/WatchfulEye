namespace WatchfulEye.Shared.MessageLibrary;

using LettuceTalk.Core;
using LettuceTalk.Core.MessageBuilders;

public class DebugJsonMessageBuilder : IMessageBuilder {

    /// <summary>
    /// Converts binary data to a message via JSON, assumes first byte is for message code via LettuceTalk Protocol
    /// </summary>
    /// <param name="data">the message data + message code</param>
    /// <returns>the converted message</returns>
    public Message FromData(byte[] data) {
        int messageCode = Convert.ToInt32(data[0]);
        byte[] jsonData = new byte[data.Length-1];
        Buffer.BlockCopy(data, 1, jsonData, 0, data.Length-1);
        
        return FromData(messageCode, jsonData);
    }

    public Message FromData(int messageCode, byte[] data) {
        Type messageType = MessageFactory.GetMessageType(messageCode);
        string jsonString = System.Text.Encoding.UTF8.GetString(data);
        Message message = (Message)System.Text.Json.JsonSerializer.Deserialize(jsonString, messageType);

        return message;
    }

    public byte[] ToData(Message message) {
        string jsonString = System.Text.Json.JsonSerializer.Serialize(message, message.GetType());
        byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(jsonString);
        byte messageCode = Convert.ToByte(MessageFactory.GetMessageCode(message));
        byte[] messageData = new byte[jsonData.Length+1];
        messageData[0] = messageCode;
        Buffer.BlockCopy(jsonData, 0, messageData, 1, jsonData.Length);

        return messageData;
    }
}