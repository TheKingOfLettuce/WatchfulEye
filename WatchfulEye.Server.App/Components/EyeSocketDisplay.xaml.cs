using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WatchfulEye.Server.Eyes;

namespace WatchfulEye.Server.App.Components;

public partial class EyeSocketDisplay : UserControl
{
    private const float PollTime = 30;
    
    private EyeSocket _eyeSocket;
    private CancellationTokenSource _loopCancel;
    
    public EyeSocketDisplay()
    {
        InitializeComponent();
        _loopCancel = new CancellationTokenSource();
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
    }

    private async void PollThumbnail(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Thread.Sleep(TimeSpan.FromSeconds(PollTime));
            if (token.IsCancellationRequested)
                break;
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
}