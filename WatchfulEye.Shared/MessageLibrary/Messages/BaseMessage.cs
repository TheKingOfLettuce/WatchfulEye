using System.Text;
using System.Text.Json;

namespace WatchfulEye.Shared.MessageLibrary.Messages;

public abstract class BaseMessage {
    public abstract MessageCodes MessageCode {get;}

    public virtual byte[] ToData() {
        byte[] messageData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, this.GetType()));
        byte[] toReturn = new byte[messageData.Length+1];
        Array.Copy(messageData, 0, toReturn, 1, messageData.Length);
        toReturn[0] = (byte)MessageCode;
        return toReturn;
    }
}