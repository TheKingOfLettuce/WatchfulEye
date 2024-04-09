namespace WatchfulEye.Shared.MessageLibrary;

public enum MessageCodes : byte {
    NONE,
    REGISTER_EYE,
    REGISTER_EYE_ACK,
    REQUEST_STREAM,
    HEARTBEAT,
    HEARTBEAT_ACK,
    DEREGISTER_EYE,
    REQUEST_PICTURE,
    TOGGLE_BUSY
}