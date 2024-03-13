namespace WatchfulEye.Server;

using WatchfulEye.Utility;
using WatchfulEye.Server.Eyes;
using LibVLCSharp.Shared;

public static class VLCLauncer {
    public static async Task ConnectToVision(EyeSocket eye, float delaySeconds) {
        using var libvlc = new LibVLC();
        Logging.Debug("Getting video data stream from eye");
        using StreamMediaInput input = new StreamMediaInput(await eye.GetDataStreamAsync());
        Logging.Debug("Creating media from video stream");
        using Media stream = new Media(libvlc, input);
        using MediaPlayer player = new MediaPlayer(stream);
        Logging.Debug("Playing stream into player");
        player.Play();

        await Task.Delay((int)(delaySeconds * 1000));
        player.Stop();
        return;
    }

    public static async Task HostStream(Stream videoStream, float delaySeconds) {
        using var libvlc = new LibVLC();
        using StreamMediaInput input = new StreamMediaInput(videoStream);
        Logging.Debug("Creating media from video stream");
        using Media stream = new Media(libvlc, input);
        using MediaPlayer player = new MediaPlayer(stream);
        Logging.Debug("Playing stream into player");
        player.Play();

        await Task.Delay((int)(delaySeconds * 1000));
        player.Stop();
        return;
    }
}