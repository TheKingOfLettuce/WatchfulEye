namespace WatchfulEye.Shared.MessageLibrary;

public static class MessageCodes {
    public const int NONE = 0;
    public const int REGISTER_EYE = 1;
    public const int REGISTER_EYE_ACK = 2;
    public const int REQUEST_STREAM = 3;
    public const int HEARTBEAT = 4;
    public const int HEARTBEAT_ACK = 5;
    public const int DEREGISTER_EYE = 6;
    public const int REQUEST_PICTURE = 7;
    public const int VISION_READY = 8;
}