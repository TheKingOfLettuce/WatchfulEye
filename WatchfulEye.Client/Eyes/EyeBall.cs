using System.Diagnostics;
using System.Runtime.InteropServices;
using NetMQ;
using NetMQ.Sockets;
using WatchfulEye.Shared.MessageLibrary;
using WatchfulEye.Shared.MessageLibrary.MessageHandlers;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Utility;

namespace WatchfulEye.Client.Eyes;

public class EyeBall {
    public readonly string EyeName;

    private DealerSocket _client;
    private ZeroMQMessageHandler _handler;
    private NetMQPoller _poller;
    
    public EyeBall(string eyeName) {
        EyeName = eyeName;
        _client = new DealerSocket();
        _handler = new ZeroMQMessageHandler();
        _poller = new NetMQPoller();
        SubscribeMessages();
        _poller.Add(_client);
        _poller.RunAsync();
        Logging.Info($"New eye ball created {EyeName}");
    }

    private void SubscribeMessages() {
        _client.ReceiveReady += _handler.HandleMessageReceived;

        _handler.Subscribe<RegisterEyeAckMessage>(HandleRegisterEyeAck);
        _handler.Subscribe<RequestStreamMessage>(HandleStreamRequest);
    }

    public void SocketEye() {
        _client.Connect("tcp://localhost:8000");
        Logging.Debug("Attempting to socket eye");
        RegisterEyeMessage message = new RegisterEyeMessage(EyeName);
        _client.SendFrame(message.ToData());
    }

    private void HandleRegisterEyeAck(RegisterEyeAckMessage msg) {
        Logging.Debug($"Received ack back from Socket {msg.PipeName} Port {msg.Port}");
        _client.Disconnect("tcp://localhost:8000");
        Logging.Debug("Connecting to eye socket");
        _client.Connect($"tcp://localhost:{msg.Port}");
        Logging.Info("Connected to eye socket");
    }

    private void HandleStreamRequest(RequestStreamMessage message) {
        Logging.Info($"Got Stream Request: {message.StreamLength}");
        Task.Run(() => StreamVideo(message));
    }

    private async Task StreamVideo(RequestStreamMessage message) {
        Logging.Debug($"Starting up a video stream python process");

        ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = "python",
            ArgumentList = {
                Path.Combine(Directory.GetCurrentDirectory(), "PythonScripts", "StreamVideo.py"),
                message.VideoWidth.ToString(),
                message.VideoHeight.ToString(),
                message.Framerate.ToString(),
                "127.0.0.0",
                message.Port.ToString(),
                message.StreamLength.ToString()
            },
            CreateNoWindow = false
        };

        Logging.Debug("Starting python stream");
        using Process pythonStream = Process.Start(startInfo);
        await pythonStream.WaitForExitAsync();
        Logging.Debug("Python stream finished");
    }
}