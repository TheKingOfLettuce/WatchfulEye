using System.Net;
using System.Net.Sockets;
using WatchfulEye.Server;
using WatchfulEye.Server.Eyes;
using WatchfulEye.Utility;

namespace WatchfulEye;

internal static class Program {

    public static async Task Main(string[] args) {
        Logging.Info("Starting the WatchfulEye");
        EyeManager.StartNetworkDiscovery();
        await ConsoleStuff();
        EyeManager.Dispose();
    }

    public static async Task ConsoleStuff() {
        int userResp = 0;
        while (userResp != -1) {
            Console.WriteLine("\n\nEnter an option below:\n");
            Console.WriteLine("1. Request Stream from all eyes");
            Console.WriteLine("2. Request Thumbnails from all eyes");
            Console.WriteLine("0. Quit");

            if (!int.TryParse(Console.ReadLine(), out userResp))
                userResp = -2;
            
            switch (userResp) {
                case 1:
                    EyeManager.ViewAllVision();
                    break;
                case 2:
                    EyeManager.GetLatestThumbnails();
                    break;
                case 0:
                    Logging.Debug("Quitting server");
                    return;
            }
        }
    }
}