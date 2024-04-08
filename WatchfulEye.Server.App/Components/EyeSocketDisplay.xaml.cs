using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using WatchfulEye.Server.Eyes;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using WatchfulEye.Utility;

namespace WatchfulEye.Server.App.Components;

public partial class EyeSocketDisplay : UserControl
{
    private const float PollTime = 60;
    
    private EyeSocket? _eyeSocket;
    private CancellationTokenSource _loopCancel;
    private LibVLC _vlc;
    private MediaPlayer _mediaPlayer;
    private bool _showingStream;
    
    public EyeSocketDisplay()
    {
        InitializeComponent();
        _loopCancel = new CancellationTokenSource();
        _vlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_vlc);
        Video.Loaded += (sender, e) => Video.MediaPlayer = _mediaPlayer;
        Thumbnail.MouseLeftButtonDown += (sender, args) => ViewStream();
    }

    public void AssignEyeSocket(EyeSocket eye)
    {
        _eyeSocket = eye;
        eye.OnThumbnailSaved += SetThumbnail;
        CancellationToken token = _loopCancel.Token;
        Task.Run( () => PollThumbnail(token), token);
    }

    public void RemoveEyeSocket()
    {
        if (_eyeSocket == default) return;

        _eyeSocket.OnThumbnailSaved -= SetThumbnail;
        _loopCancel.Cancel();
        _eyeSocket = default;
        Thumbnail.Visibility = Visibility.Hidden;
        Video.Visibility = Visibility.Hidden;
    }

    private async void PollThumbnail(CancellationToken token)
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        _eyeSocket.RequestThumbnail();
        while (!token.IsCancellationRequested)
        {
            Thread.Sleep(TimeSpan.FromSeconds(PollTime));
            if (token.IsCancellationRequested)
                break;
            if (_showingStream)
                continue;
            _eyeSocket.RequestThumbnail();
        }
    }

    private void SetThumbnail()
    {
        Thumbnail.Dispatcher.Invoke(SetThumbnailAsync);
    }

    private void SetThumbnailAsync()
    {
        Thumbnail.Source = GetImage();
        Thumbnail.Visibility = Visibility.Visible;
    }

    private BitmapImage GetImage()
    {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        byte[] data =
            File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Thumbnails", _eyeSocket.EyeName + ".jpg"));
        MemoryStream stream = new MemoryStream(data);
        image.StreamSource = stream;
        image.EndInit();
        return image;
    }

    public void ViewStream()
    {
        if (_eyeSocket == null || _showingStream) return;
        
        _eyeSocket.StartVision();
        Video.Visibility = Visibility.Visible;
        Task.Run(() => ConnectToVision(_eyeSocket, 15 + 5));
    }
    
    /// <summary>
    /// Helper method to create a VLC stream to view live video from a <see cref="EyeSocket"/>
    /// </summary>
    /// <param name="eye">the eye socket with a live video stream</param>
    /// <param name="delaySeconds">how long to hold the connection for</param>
    public async Task ConnectToVision(EyeSocket eye, float delaySeconds) {
        Logging.Debug("Getting video data stream from eye");
        Stream? eyeVisionStream = await eye.GetNetworkStreamAsync();
        if (eyeVisionStream == null) {
            Logging.Warning("Received null stream from eye vision");
            return;
        }

        _showingStream = true;
        
        await HostStream(eyeVisionStream, delaySeconds);
    }

    /// <summary>
    /// Helper method to create a VLC stream
    /// </summary>
    /// <param name="videoStream">the stream of video data</param>
    /// <param name="delaySeconds">how long to play the video stream for</param>
    public async Task HostStream(Stream videoStream, float delaySeconds) {
        using StreamMediaInput input = new StreamMediaInput(videoStream);
        Logging.Debug("Creating media from video stream");
        using Media stream = new Media(_vlc, input);
        Logging.Debug("Playing stream into player");
        _mediaPlayer.Play(stream);

        await Task.Delay((int)(delaySeconds * 1000));
        Logging.Debug("Stopping stream player");
        _mediaPlayer.Stop();
        Video.Dispatcher.Invoke(HideVideo);
    }

    private void HideVideo()
    {
        _showingStream = false;
        Video.Visibility = Visibility.Hidden;
    }
}