using LibVLCSharp.Shared;
using WatchfulEye.Eyes;
using WatchfulEye.Utility;

namespace WatchfulEye;

internal static class Program {
    public static void Main(string[] args) {
        Logging.Info("Starting the WatchfulEye");

        Task.Run(TestEye);
    }

    private static async Task TestEye() {
        Logging.Info("Creating test eye socket");
        EyeSocket eye = new EyeSocket();

        using var libvlc = new LibVLC(enableDebugLogs: true);
        Logging.Debug("Getting video data stream from eye");
        using StreamMediaInput input = new StreamMediaInput(await eye.GetDataStreamAsync());
        Logging.Debug("Creating media from video stream");
        using Media stream = new Media(libvlc, input);
        using MediaPlayer player = new MediaPlayer(stream);
        Logging.Debug("Playing stream into player");
        player.Play();

        await Task.Delay(10000);
        return;
    }
}