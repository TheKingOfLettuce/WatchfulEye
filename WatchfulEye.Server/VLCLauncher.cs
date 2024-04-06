namespace WatchfulEye.Server;

using WatchfulEye.Utility;
using WatchfulEye.Server.Eyes;
using LibVLCSharp.Shared;

public static class VLCLauncer {

    /// <summary>
    /// Helper method to create a VLC stream to view live video from a <see cref="EyeSocket"/>
    /// </summary>
    /// <param name="eye">the eye socket with a live video stream</param>
    /// <param name="delaySeconds">how long to hold the connection for</param>
    public static async Task ConnectToVision(EyeSocket eye, float delaySeconds) {
        Logging.Debug("Getting video data stream from eye");
        Stream? eyeVisionStream = await eye.GetDataStreamAsync();
        if (eyeVisionStream == null) {
            Logging.Warning("Received null stream from eye vision");
            return;
        }
        
        await HostStream(eyeVisionStream, delaySeconds);
        return;
    }

    /// <summary>
    /// Helper method to create a VLC stream
    /// </summary>
    /// <param name="videoStream">the stream of video data</param>
    /// <param name="delaySeconds">how long to play the video stream for</param>
    public static async Task HostStream(Stream videoStream, float delaySeconds) {
        using var libvlc = new LibVLC();
        using StreamMediaInput input = new StreamMediaInput(videoStream);
        Logging.Debug("Creating media from video stream");
        using Media stream = new Media(libvlc, input);
        using MediaPlayer player = new MediaPlayer(stream);
        Logging.Debug("Playing stream into player");
        player.Play();

        await Task.Delay((int)(delaySeconds * 1000));
        Logging.Debug("Stopping stream player");
        player.Stop();
        return;
    }
}