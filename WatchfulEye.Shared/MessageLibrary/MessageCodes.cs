namespace WatchfulEye.Shared.MessageLibrary;

public enum MessageCodes : byte {
    NONE,
    REGISTER_EYE,
    REGISTER_EYE_ACK,
    REQUEST_STREAM,
    HEARTBEAT,
    HEARTBEAT_ACK
}