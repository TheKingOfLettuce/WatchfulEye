using NetMQ;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages;

namespace WatchfulEye.Shared;

public class HeartbeatMonitor {
    private NetMQSocket _socket;
    private ZeroMQMessageHandler _handler;
    private AutoResetEvent _heartbeatAck;
    private bool _sendAckOnLoopStart = true;

    private readonly TimeSpan _acknowledgementWaitTime;
    private readonly TimeSpan _nextAckowledgementTime;

    public HeartbeatMonitor(NetMQSocket socket, ZeroMQMessageHandler handler) {

    }

    private async Task HearbeatLoop() {
        if (!_sendAckOnLoopStart)
            await Task.Delay(_nextAckowledgementTime);
        byte[] heartbeatData = new HeartbeatMessage().ToData();
        while (true) {
            if (!_socket.TrySendFrame(_acknowledgementWaitTime, heartbeatData, heartbeatData.Length)) {
                return;
            }
            _heartbeatAck.Reset();
            if (_heartbeatAck.WaitOne(_acknowledgementWaitTime)) {
                await Task.Delay(_nextAckowledgementTime);
            }
            else {
                return;
            }
        }
    }

    private void HandleHeartbeat(HeartbeatMessage message) {
        byte[] data = new HeartbeatAckMessage().ToData();
        _socket.SendFrame(data, data.Length);
    }

    private void HandleHeartbeatAck(HeartbeatAckMessage message) {
        _heartbeatAck.Set();
    }
}