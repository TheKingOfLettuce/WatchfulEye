using System.Text.Json.Serialization;

namespace WatchfulEye.Shared.MessageLibrary.Messages.VisionRequests;

public class RequestPictureMessage : VisionRequestMessage {
    public override MessageCodes MessageCode => MessageCodes.REQUEST_PICTURE;
    public override VisionRequestType VisionRequestType => VisionRequestType.Picture;

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