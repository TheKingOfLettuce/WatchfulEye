using NetMQ;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages;

namespace WatchfulEye.Shared;

public class HeartbeatMonitor : IDisposable {
    public event Action OnHeartBeat;
    public event Action OnHeartBeatFail;

    private bool _sendAckOnLoopStart = false;
    private TimeSpan _timeoutTime;
    private TimeSpan _nextAckTime;
    private bool _isActive;

    private readonly NetMQSocket _socket;
    private readonly ZeroMQMessageHandler _handler;
    private readonly AutoResetEvent _heartbeatAck;
    
    private readonly CancellationTokenSource _loopToken;

    public HeartbeatMonitor(NetMQSocket socket, ZeroMQMessageHandler handler, float timeout = 10, float nextAck = 60) {
        _socket = socket;
        _handler = handler;
        _heartbeatAck = new AutoResetEvent(false);
        _loopToken = new CancellationTokenSource();

        _timeoutTime = TimeSpan.FromSeconds(timeout);
        _nextAckTime = TimeSpan.FromSeconds(nextAck);
    }

    public void StartMonitor() {
        if (_isActive) return;

        _isActive = true;
        _handler.Subscribe<HeartbeatMessage>(HandleHeartbeat);
        _handler.Subscribe<HeartbeatAckMessage>(HandleHeartbeatAck);

        CancellationToken cancel = _loopToken.Token;
        Task.Run(() => HearbeatLoop(cancel), _loopToken.Token);
    }

    public void StopMonitor() {
        if (!_isActive) return;

        _isActive = false;
        _handler.Unsubscribe<HeartbeatMessage>(HandleHeartbeat);
        _handler.Unsubscribe<HeartbeatAckMessage>(HandleHeartbeatAck);
        _loopToken.Cancel();
    }

    private async Task HearbeatLoop(CancellationToken token) {
        if (!_sendAckOnLoopStart)
            await Task.Delay(_nextAckTime);
        byte[] heartbeatData = new HeartbeatMessage().ToData();
        while (!token.IsCancellationRequested) {
            if (!_socket.TrySendFrame(_timeoutTime, heartbeatData, heartbeatData.Length)) {
                // heartbeat failed to send
                break;
            }
            _heartbeatAck.Reset();
            if (_heartbeatAck.WaitOne(_timeoutTime)) {
                // received ack from heartbeat
                OnHeartBeat?.Invoke();
                await Task.Delay(_nextAckTime);
            }
            else {
                // did not receive ack
                break;
            }
        }

        if (!token.IsCancellationRequested) {
            // handle heartbeat failure here
            OnHeartBeatFail?.Invoke();
            StopMonitor();
        }
    }

    private void HandleHeartbeat(HeartbeatMessage message) {
        byte[] data = new HeartbeatAckMessage().ToData();
        _socket.SendFrame(data, data.Length);
    }

    private void HandleHeartbeatAck(HeartbeatAckMessage message) {
        _heartbeatAck.Set();
    }

    public void Dispose() {
        GC.SuppressFinalize(this);

        StopMonitor();
        _heartbeatAck.Dispose();
        _loopToken.Dispose();
    }
}