using LettuceTalk.Core.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages.General;

namespace WatchfulEye.Shared.MessageLibrary;

/// <summary>
/// Checks the pulse of who we are talking to
/// </summary>
public class HeartbeatMonitor : IDisposable {
    public event Action? OnHeartBeat;
    public event Action? OnHeartBeatFail;

    private bool _sendAckOnLoopStart = false;
    private TimeSpan _timeoutTime;
    private TimeSpan _nextAckTime;
    private bool _isActive;

    private readonly TalkingPoint _comms;
    private readonly AutoResetEvent _heartbeatAck;
    private readonly CancellationTokenSource _loopToken;

    public HeartbeatMonitor(TalkingPoint comms, float timeout = 10, float nextAck = 60) {
        _comms = comms;
        _heartbeatAck = new AutoResetEvent(false);
        _loopToken = new CancellationTokenSource();

        _timeoutTime = TimeSpan.FromSeconds(timeout);
        _nextAckTime = TimeSpan.FromSeconds(nextAck);
    }

    /// <summary>
    /// Starts our Heartbeat loop, ensuring only started once
    /// </summary>
    public void StartMonitor() {
        if (_isActive) return;

        _isActive = true;
        _comms.Subscribe<HeartbeatMessage>(HandleHeartbeat);
        _comms.Subscribe<HeartbeatAckMessage>(HandleHeartbeatAck);

        CancellationToken cancel = _loopToken.Token;
        Task.Run(() => HeartbeatLoop(cancel), _loopToken.Token);
    }

    /// <summary>
    /// Stops our heartbeat loop, does nothing if already stopped
    /// </summary>
    public void StopMonitor() {
        if (!_isActive) return;

        _isActive = false;
        _comms.Unsubscribe<HeartbeatMessage>(HandleHeartbeat);
        _comms.Unsubscribe<HeartbeatAckMessage>(HandleHeartbeatAck);
        _loopToken.Cancel();
    }

    /// <summary>
    /// Ours heartbeat loop, sending a heart beat message and waiting for an ack
    /// </summary>
    /// <param name="token">the cancel token to cancel gracefully</param>
    private async Task HeartbeatLoop(CancellationToken token) {
        if (!_sendAckOnLoopStart)
            await DelayHelper(_nextAckTime, token);
        while (!token.IsCancellationRequested) {
            _comms.SendMessage(new SendMessageArgs(new HeartbeatMessage()));
            if (_heartbeatAck.WaitOne(_timeoutTime)) {
                OnHeartBeat?.Invoke();
                _heartbeatAck.Reset();
                await DelayHelper(_nextAckTime, token);
            }
            else {
                break;
            }
        }

        if (!token.IsCancellationRequested) {
            // handle heartbeat failure here
            OnHeartBeatFail?.Invoke();
            StopMonitor();
        }
    }

    /// <summary>
    /// Delay helper, to help with handling <see cref="OperationCanceledException"/>
    /// </summary>
    /// <param name="time">the time to delay for</param>
    /// <param name="token">the forwarded <see cref="CancellationToken"/></param>
    private static async Task DelayHelper(TimeSpan time, CancellationToken token) {
        try {
            await Task.Delay(time, token);
        }
        catch (OperationCanceledException) {
            
        }

        return;
    }

    /// <summary>
    /// Handles sending the <see cref="HeartbeatMessage"/>
    /// </summary>
    /// <param name="message">the <see cref="HeartbeatMessage"/> to send</param>
    private void HandleHeartbeat(HeartbeatMessage message) {
        _comms.SendMessage(new SendMessageArgs(new HeartbeatAckMessage()));
    }

    /// <summary>
    /// Handles receiving a <see cref="HeartbeatAckMessage"/>
    /// </summary>
    /// <param name="message">the <see cref="HeartbeatAckMessage"/></param>
    private void HandleHeartbeatAck(HeartbeatAckMessage message) {
        _heartbeatAck.Set();
    }

    /// <summary>
    /// Handles canceling our monitor loop
    /// </summary>
    public void Dispose() {
        GC.SuppressFinalize(this);

        StopMonitor();
        _heartbeatAck.Dispose();
        _loopToken.Dispose();
    }
}