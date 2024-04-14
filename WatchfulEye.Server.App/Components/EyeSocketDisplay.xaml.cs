using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibVLCSharp.Shared;
using WatchfulEye.Server.Eyes;
using WatchfulEye.Shared.MessageLibrary.Messages.VisionRequests;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using WatchfulEye.Shared.Utility;
using System.Net.Sockets;

namespace WatchfulEye.Server.App.Components;

public partial class EyeSocketDisplay : UserControl
{
    private const float PollTime = 60;
    
    private static readonly LibVLC Vlc = new LibVLC();
    
    private EyeSocket? _eyeSocket;
    private readonly CancellationTokenSource _loopCancel;
    private readonly MediaPlayer _mediaPlayer;
    private StreamMediaInput? _mediaInput;
    private Stream? _visionStream;
    
    public EyeSocketDisplay()
    {
        InitializeComponent();
        _loopCancel = new CancellationTokenSource();
        _mediaPlayer = new MediaPlayer(Vlc);
        Video.Loaded += (sender, e) => Video.MediaPlayer = _mediaPlayer;
        Thumbnail.MouseLeftButtonDown += (sender, args) => ViewStream();
    }

    public void AssignEyeSocket(EyeSocket eye)
    {
        _eyeSocket = eye;
        _eyeSocket.OnVisionReady += HandleVisionReady;
        CancellationToken token = _loopCancel.Token;
        Task.Run( () => PollThumbnail(token), token);
    }

    public void RemoveEyeSocket()
    {
        if (_eyeSocket == default) return;

        _eyeSocket.OnVisionReady -= HandleVisionReady;
        _loopCancel.Cancel();
        _eyeSocket = default;
        // TODO set to default image instead of hiding
        Thumbnail.Visibility = Visibility.Hidden;
        Video.Visibility = Visibility.Hidden;
    }

    private async void PollThumbnail(CancellationToken token)
    {
        // TODO see if we actually need to wait
        // sanity
        await Task.Delay(TimeSpan.FromSeconds(5), token);
        Dispatcher.Invoke(RequestThumbnail);
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(PollTime), token);
            if (token.IsCancellationRequested)
                break;
            Dispatcher.Invoke(RequestThumbnail);
        }
    }

    private void RequestThumbnail() => _eyeSocket?.RequestPicture((int)Width, (int)Height);
    private void ViewStream() => _eyeSocket?.RequestStream();
    private void HandleVisionReady(VisionRequestType requestType) => Task.Run(() => HandleVisionReadyAsync(requestType));

    private async void HandleVisionReadyAsync(VisionRequestType requestType)
    {
        if (_eyeSocket == null) return;
        _visionStream = await _eyeSocket.GetNetworkStreamAsync();
        if (_visionStream == null) {
            Logging.Error($"Failed to get network stream from vision for request {requestType}");
            return;
        }

        Logging.Info($"Handle vision ready for request {requestType} for eye {_eyeSocket.EyeName}");
        switch (requestType)
        {
            case VisionRequestType.Stream:
                HostStream();
                break;
            case VisionRequestType.Picture:
                SaveThumbnail();
                break;
            case VisionRequestType.None:
            default:
                return;
        }
    }

    private void SetThumbnail(ImageSource image)
    {
        Logging.Info($"Setting thumbnail image for eye {_eyeSocket?.EyeName}");
        Thumbnail.Source = image;
        // TODO find placeholder image for nothing so we dont have to set visible every time
        Thumbnail.Visibility = Visibility.Visible;
    }
    
    private async void SaveThumbnail()
    {
        Logging.Info($"Creating thumbnail data for eye {_eyeSocket?.EyeName}");
        MemoryStream memoryStream = new MemoryStream();
        await _visionStream.CopyToAsync(memoryStream);
        _visionStream.Dispose();
        memoryStream.Seek(0, SeekOrigin.Begin);
        Video.Dispatcher.Invoke(() => SetThumbnail(GetImage(memoryStream)));
    }

    private static BitmapImage GetImage(Stream memoryStream)
    {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = memoryStream;
        image.EndInit();
        return image;
    }

    /// <summary>
    /// Helper method to create a VLC stream
    /// </summary>
    /// <param name="videoStream">the stream of video data</param>
    private void HostStream()
    {
        _mediaInput = new StreamMediaInput(_visionStream);
        Logging.Debug("Creating media from video stream");
        Media stream = new Media(Vlc, _mediaInput);
        Logging.Debug("Playing stream into player");
        Video.Dispatcher.Invoke(ShowVideo);
        _mediaPlayer.Stopped += PlayerStopped;
        _mediaPlayer.Play(stream);
    }

    private void PlayerStopped(object? sender, EventArgs args) {
        _mediaPlayer.Stopped -= PlayerStopped;
        _mediaPlayer.Media?.Dispose();
        _mediaInput?.Dispose();
        _visionStream?.Dispose();
        _mediaInput = null;
        Logging.Info($"Player stopped for Eye {_eyeSocket?.EyeName}");
        Video.Dispatcher.Invoke(HideVideo);
    }
    
    private void HideVideo() => Video.Visibility = Visibility.Hidden;
    private void ShowVideo() => Video.Visibility = Visibility.Visible;
}