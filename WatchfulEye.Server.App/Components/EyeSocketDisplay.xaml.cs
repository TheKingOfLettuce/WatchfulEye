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
    
    public EyeSocketDisplay() {
        InitializeComponent();
        _loopCancel = new CancellationTokenSource();
        _mediaPlayer = new MediaPlayer(Vlc);
        Video.Loaded += (sender, e) => Video.MediaPlayer = _mediaPlayer;
        Thumbnail.MouseLeftButtonDown += (sender, args) => ViewStream();
    }

    /// <summary>
    /// Assign a new EyeSocket to this display and listens for vision
    /// </summary>
    /// <param name="eye">the eye to add</param>
    public void AssignEyeSocket(EyeSocket eye) {
        _eyeSocket = eye;
        _eyeSocket.OnVisionReady += HandleVisionReady;
        _eyeSocket.OnHeartBeatPulse += HandleHeartbeat;
        CancellationToken token = _loopCancel.Token;
        Task.Run( () => PollThumbnail(token), token);
        Dispatcher.Invoke(() => WriteStatus($"New eye connected {eye.Name}", Brushes.Green));
    }

    /// <summary>
    /// Removes an eye from this display, clearing any loops
    /// </summary>
    public void RemoveEyeSocket() {
        if (_eyeSocket == default) return;

        _eyeSocket.OnVisionReady -= HandleVisionReady;
        _eyeSocket.OnHeartBeatPulse -= HandleHeartbeat;
        _loopCancel.Cancel();
        _eyeSocket = default;
        // TODO set to default image instead of hiding
        Thumbnail.Dispatcher.Invoke(() => Thumbnail.Visibility = Visibility.Hidden);
        Video.Dispatcher.Invoke(HideVideo);
        Dispatcher.Invoke(() => WriteStatus("No Eye Connected", Brushes.Red));
    }

    private void HandleHeartbeat() {
        Dispatcher.Invoke(() => WriteStatus("Camera idle", Brushes.Green));
    }

    /// <summary>
    /// Polls the eye continously for a thumbnail for partial activity
    /// </summary>
    /// <param name="token">the token to cancel this loop with</param>
    private async void PollThumbnail(CancellationToken token) {
        try {
            Dispatcher.Invoke(RequestThumbnail);
            while (!token.IsCancellationRequested) {
                await Task.Delay(TimeSpan.FromSeconds(PollTime), token);
                if (token.IsCancellationRequested)
                    break;
                Dispatcher.Invoke(RequestThumbnail);
            }
        }
        catch (OperationCanceledException e) {
            Logging.Warning("Delay task was canceled in poll for thumbnail", e);
            return;
        }
    }

    private void RequestThumbnail() {
        WriteStatus("Polling for thumbnail", Brushes.Green);
        _eyeSocket?.RequestPicture((int)Width, (int)Height);
    }
    private void ViewStream() {
        Dispatcher.Invoke(() => WriteStatus("Requesting for live stream", Brushes.Green));
        _eyeSocket?.RequestStream();
    }
    private void HandleVisionReady(VisionRequestType requestType) => Task.Run(() => HandleVisionReadyAsync(requestType));

    /// <summary>
    /// Handles receiving a vision ready message from our socket
    /// </summary>
    /// <param name="requestType">the vision type</param>
    private async void HandleVisionReadyAsync(VisionRequestType requestType)
    {
        if (_eyeSocket == null) return;
        _visionStream = await _eyeSocket.GetNetworkStreamAsync();
        if (_visionStream == null) {
            Logging.Error($"Failed to get network stream from vision for request {requestType}");
            Dispatcher.Invoke(() => WriteStatus("Data Stream Request Failed", Brushes.Red));
            return;
        }

        Logging.Info($"Handle vision ready for request {requestType} for eye {_eyeSocket.Name}");
        switch (requestType) {
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

    /// <summary>
    /// Sets the thumbnail to the given image
    /// </summary>
    /// <param name="image">the image to set</param>
    private void SetThumbnail(ImageSource image) {
        Logging.Info($"Setting thumbnail image for eye {_eyeSocket?.Name}");
        WriteStatus("Thumbnail downloaded", Brushes.Green);
        Thumbnail.Source = image;
        // TODO find placeholder image for nothing so we dont have to set visible every time
        Thumbnail.Visibility = Visibility.Visible;
    }
    
    /// <summary>
    /// Saves the vision stream into image data
    /// </summary>
    private async void SaveThumbnail() {
        Logging.Info($"Creating thumbnail data for eye {_eyeSocket?.Name}");
        Dispatcher.InvokeAsync(() => WriteStatus("Downloading thumbnail", Brushes.Green));
        MemoryStream memoryStream = new MemoryStream();
        await _visionStream.CopyToAsync(memoryStream);
        _visionStream.Dispose();
        memoryStream.Seek(0, SeekOrigin.Begin);
        Video.Dispatcher.Invoke(() => SetThumbnail(GetImage(memoryStream)));
    }

    /// <summary>
    /// Takes a stream and turns into a <see cref="BitmapImage"/>
    /// </summary>
    /// <param name="stream">the stream to save</param>
    /// <returns>a <see cref="BitmapImage"/> from stream data</returns>
    private static BitmapImage GetImage(Stream stream) {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        return image;
    }

    /// <summary>
    /// Passes our vision stream into a VLC stream
    /// </summary>
    private void HostStream() {
        _mediaInput = new StreamMediaInput(_visionStream);
        Logging.Debug("Creating media from video stream");
        Media stream = new Media(Vlc, _mediaInput);
        Logging.Debug("Playing stream into player");
        Video.Dispatcher.Invoke(ShowVideo);
        Dispatcher?.Invoke(() => WriteStatus("Live stream received", Brushes.Green));
        _mediaPlayer.Stopped += PlayerStopped;
        _mediaPlayer.Play(stream);
    }

    /// <summary>
    /// Handles when our VLC player stops, or our vision stream ends
    /// </summary>
    /// <param name="sender">unused</param>
    /// <param name="args">unsed</param>
    private void PlayerStopped(object? sender, EventArgs args) {
        _mediaPlayer.Stopped -= PlayerStopped;
        _mediaPlayer.Media?.Dispose();
        _mediaInput?.Dispose();
        _visionStream?.Dispose();
        _mediaInput = null;
        Logging.Info($"Player stopped for Eye {_eyeSocket?.Name}");
        Video.Dispatcher.Invoke(HideVideo);
        Dispatcher?.Invoke(() => WriteStatus("Camera live ended", Brushes.Green));
    }
    
    private void HideVideo() => Video.Visibility = Visibility.Hidden;
    private void ShowVideo() => Video.Visibility = Visibility.Visible;

    private void WriteStatus(string message, SolidColorBrush color) {
        StatusText.Content = message;
        StatusText.Foreground = color;
    }
}