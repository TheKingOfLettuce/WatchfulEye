using System.Text.Json.Serialization;

namespace WatchfulEye.Shared.MessageLibrary.Messages;

public class PictureMessage : BaseMessage {
    public override MessageCodes MessageCode => MessageCodes.PICTURE;

    [JsonInclude]
    public readonly byte[] PictureData;

    [JsonConstructor]
    public PictureMessage(byte[] pictureData) {
        PictureData = pictureData;
    }
}