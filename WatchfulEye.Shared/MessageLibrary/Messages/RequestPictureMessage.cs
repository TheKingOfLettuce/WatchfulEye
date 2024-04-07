using System.Text.Json.Serialization;

namespace WatchfulEye.Shared.MessageLibrary.Messages;

public class RequestPictureMessage : BaseMessage {
    public override MessageCodes MessageCode => MessageCodes.REQUEST_PICTURE;

    [JsonInclude]
    public readonly int Port;
    [JsonInclude]
    public readonly int PictureWidth;
    [JsonInclude]
    public readonly int PictureHeight;

    [JsonConstructor]
    public RequestPictureMessage(int port, int pictureWidth, int pictureHeight) {
        Port = port;
        PictureWidth = pictureWidth;
        PictureHeight = pictureHeight;
    }
}