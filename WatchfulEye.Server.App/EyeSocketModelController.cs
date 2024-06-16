using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibVLCSharp.Shared;
using WatchfulEye.Server.Eyes;
using WatchfulEye.Shared.MessageLibrary.Messages.VisionRequests;
using WatchfulEye.Shared.Utility;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace WatchfulEye.Server.App;

public class EyeSocketModelController : PropertyChangedBase {
    private const float PollTime = 60;
    private static readonly LibVLC Vlc = new LibVLC();

    #region DataBindings
    public bool ThumbnailVisibility {
        get {
            return _thumbnailVisibility;
        }
        set {
            _thumbnailVisibility = value;
            OnPropertyChanged();
        }
    }
    private bool _thumbnailVisibility;

    public bool VideoVisibility {
        get {
            return _videoVisibility;
        }
        set {
            _videoVisibility = value;
            OnPropertyChanged();
        }
    }
    private bool _videoVisibility;

    public bool NoConnectionVisibility {
        get {
            return _noConnectionVisibility;
        }
        set {
            _noConnectionVisibility = value;
            OnPropertyChanged();
        }
    }
    private bool _noConnectionVisibility;

    public bool StatusVisibility {
        get {
            return _statusVisibility;
        }
        set {
            _statusVisibility = value;
            OnPropertyChanged();
        }
    }
    private bool _statusVisibility;

    public string CurrentStatus {
        get {
            return _currentStatus ?? string.Empty;
        }
        set {
            _currentStatus = value;
            OnPropertyChanged();
        }
    }
    private string? _currentStatus;

    public SolidColorBrush StatusColor {
        get {
            return _statusColor;
        }
        set {
            _statusColor = value;
            OnPropertyChanged();
        }
    }
    private SolidColorBrush _statusColor = Brushes.Green;

    public ImageSource? ThumbnailSource {
        get {
            return _thumbnailSource;
        }
        set {
            _thumbnailSource = value;
            OnPropertyChanged();
        }
    }
    private ImageSource? _thumbnailSource;
    
    public MediaPlayer VideoSource {
        get {
            return _mediaPlayer;
        }
        set {
            _mediaPlayer = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region Commands
    class StreamRequestCommands : ICommand {
        private readonly EyeSocketModelController _controller;
        public event EventHandler? CanExecuteChanged;

        public StreamRequestCommands(EyeSocketModelController instance) {
            _controller = instance;
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _controller.ViewStream();
    }
    public ICommand StreamRequestCommand {get;}
    #endregion

    private EyeSocket? _eye;
    private readonly CancellationTokenSource _thumbnailToken;
    private Stream? _visionStream;
    private MediaPlayer _mediaPlayer;
    private StreamMediaInput? _mediaInput;

    public EyeSocketModelController() {
        StreamRequestCommand = new StreamRequestCommands(this);
        _thumbnailToken = new CancellationTokenSource();
        VideoSource = new MediaPlayer(Vlc);
        StatusVisibility = false;
        NoConnectionVisibility = true;
        ThumbnailVisibility = false;
        VideoVisibility = false;
    }

    public void ActivateEye(EyeSocket eye) {
        _eye = eye;
        NoConnectionVisibility = false;
        StatusVisibility = true;
        _eye.OnVisionReady += HandleVisionReady;
        _eye.OnHeartBeatPulse += HandleHeartbeat;
        CancellationToken token = _thumbnailToken.Token;
        Task.Run( () => PollThumbnail(token), token);
    }

    public void DeactivateEye() {
        _eye.OnVisionReady -= HandleVisionReady;
        _eye.OnHeartBeatPulse -= HandleHeartbeat;
        StatusVisibility = false;
        VideoVisibility = false;
        ThumbnailVisibility = false;
        NoConnectionVisibility = true;
        _thumbnailToken.Cancel();
        _eye = null;
    }

    #region Thumbnail Stuff
    /// <summary>
    /// Polls the eye continuously for a thumbnail for partial activity
    /// </summary>
    /// <param name="token">the token to cancel this loop with</param>
    private async void PollThumbnail(CancellationToken token) {
        try {
            RequestThumbnail();
            while (!token.IsCancellationRequested) {
                await Task.Delay(TimeSpan.FromSeconds(PollTime), token);
                if (token.IsCancellationRequested)
                    break;
                RequestThumbnail();
            }
        }
        catch (OperationCanceledException e) {
            Logging.Warning("Delay task was canceled in poll for thumbnail", e);
            return;
        }
    }

    private void RequestThumbnail() {
        CurrentStatus = "Requesting new thumbnail";
        _eye.RequestPicture(800, 450);
    }

    /// <summary>
    /// Saves the vision stream into image data
    /// </summary>
    private async void SaveThumbnail() {
        Logging.Info($"Creating thumbnail data for eye {_eye?.Name}");
        CurrentStatus = "Downloading Thumbnail";
        MemoryStream memoryStream = new MemoryStream();
        await _visionStream.CopyToAsync(memoryStream);
        _visionStream.Dispose();
        memoryStream.Seek(0, SeekOrigin.Begin);
        SetThumbnail(GetImage(memoryStream));
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
        image.Freeze();
        return image;
    }

    /// <summary>
    /// Sets the thumbnail to the given image
    /// </summary>
    /// <param name="image">the image to set</param>
    private void SetThumbnail(ImageSource image) {
        Logging.Info($"Setting thumbnail image for eye {_eye?.Name}");
        CurrentStatus = "Thumbnail Downloaded";
        ThumbnailSource = image;
        ThumbnailVisibility = true;
    }
    #endregion

    #region Stream Stuff
    private void ViewStream() {
        CurrentStatus = "Requesting for live stream";
        _eye.RequestStream();
    }

    /// <summary>
    /// Passes our vision stream into a VLC stream
    /// </summary>
    private void HostStream() {
        _mediaInput = new StreamMediaInput(_visionStream);
        Logging.Debug("Creating media from video stream");
        Media stream = new Media(Vlc, _mediaInput);
        Logging.Debug("Playing stream into player");
        VideoVisibility = true;
        CurrentStatus = "Live stream received";
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
        Logging.Info($"Player stopped for Eye {_eye?.Name}");
        VideoVisibility = false;
        CurrentStatus = "Camera live ended";
    }
    #endregion

    private void HandleHeartbeat() {
        StatusColor = Brushes.Green;
    }

    private void HandleVisionReady(VisionRequestType requestType) => Task.Run(() => HandleVisionReadyAsync(requestType));

    /// <summary>
    /// Handles receiving a vision ready message from our socket
    /// </summary>
    /// <param name="requestType">the vision type</param>
    private async void HandleVisionReadyAsync(VisionRequestType requestType)
    {
        if (_eye == null) return;
        _visionStream = await _eye.GetNetworkStreamAsync();
        if (_visionStream == null) {
            Logging.Error($"Failed to get network stream from vision for request {requestType}");
            CurrentStatus = "Data Stream Request Failed";
            StatusColor = Brushes.Red;
            return;
        }

        Logging.Info($"Handle vision ready for request {requestType} for eye {_eye.Name}");
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
}