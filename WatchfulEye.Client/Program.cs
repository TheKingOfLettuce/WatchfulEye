using WatchfulEye.Client.Eyes;
using WatchfulEye.Utility;

namespace WatchfulEye.Client;

internal static class Program {
    public static async Task Main(string[] args) {
        Logging.Debug("Creating eyeball");
        using EyeBall eye = new EyeBall(args[0]);
        eye.SocketEye();
        eye.DisconnectedWaiter.WaitOne();
        Logging.Debug("Shutting program down");
    }

    public static async Task Block() {
        Console.WriteLine("Press any key to quit");
        Console.Read();
        return;
    }
}




