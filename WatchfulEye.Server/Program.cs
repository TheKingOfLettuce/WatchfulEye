using LibVLCSharp.Shared;
using WatchfulEye.Server.Eyes;
using WatchfulEye.Shared.MessageLibrary.Messages;
using WatchfulEye.Utility;

namespace WatchfulEye;

internal static class Program {
    private static EyeManager _eyes;

    public static async Task Main(string[] args) {
        Logging.Info("Starting the WatchfulEye");

        _eyes = new EyeManager();
        await ConsoleStuff();
    }

    public static async Task ConsoleStuff() {
        int userResp = 0;
        while (userResp != -1) {
            Console.WriteLine("\n\nEnter an option below:\n");
            Console.WriteLine("1. Test sending message to client");
            Console.WriteLine("0. Quit");

            if (!int.TryParse(Console.ReadLine(), out userResp))
                userResp = -2;
            
            switch (userResp) {
                case 1:
                    _eyes.PostToAllSockets(new RequestStreamMessage(5));
                    break;
                case 0:
                    Logging.Debug("Quitting server");
                    return;
            }
        }
    } 

    // private static async Task TestEye() {
    //     Logging.Info("Creating test eye socket");
    //     EyeSocket eye = new EyeSocket();

    //     using var libvlc = new LibVLC();
    //     Logging.Debug("Getting video data stream from eye");
    //     using StreamMediaInput input = new StreamMediaInput(await eye.GetDataStreamAsync());
    //     Logging.Debug("Creating media from video stream");
    //     using Media stream = new Media(libvlc, input);
    //     using MediaPlayer player = new MediaPlayer(stream);
    //     Logging.Debug("Playing stream into player");
    //     player.Play();

    //     Logging.Debug("Waiting 30 seconds");
    //     await Task.Delay(30000);
    //     Logging.Debug("30 seconds elapsed, returning");
    //     player.Stop();
    //     return;
    // }
}