using WatchfulEye.Client.Eyes;
using WatchfulEye.Utility;

namespace WatchfulEye.Client;

internal static class Program {
    public static void Main(string[] args) {
        Logging.Debug("Creating eyeball");
        using EyeBall? eye = EyeBall.SocketEye(args[0]);
        if (eye == null) {
            Logging.Error("Could not socket eye");
            return;
        }
        eye.DisconnectedWaiter.WaitOne();
        Logging.Debug("Shutting program down");
    }
}




