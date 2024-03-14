namespace WatchfulEye.Server;

using WatchfulEye.Utility;
using WatchfulEye.Server.Eyes;
using LibVLCSharp.Shared;
using System.Net.Sockets;

public static class VLCLauncer {
    public static async Task ConnectToVision(EyeSocket eye, float delaySeconds) {
        using var libvlc = new LibVLC();
        Logging.Debug("Getting video data stream from eye");
        Stream eyeVisionStream = await eye.GetDataStreamAsync();
        if (eyeVisionStream == null) {
            Logging.Warning("Received null stream from eye vision");
            return;
        }
        using StreamMediaInput input = new StreamMediaInput(eyeVisionStream);
        Logging.Debug("Creating media from video stream");
        using Media stream = new Media(libvlc, input);
        using MediaPlayer player = new MediaPlayer(stream);
        Logging.Debug("Playing stream into player");
        player.Play();

        await Task.Delay((int)(delaySeconds * 1000));
        player.Stop();
        Logging.Debug("Stopping stream player");
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