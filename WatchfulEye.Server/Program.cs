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
            Console.WriteLine("1. Test sending message to client");
            Console.WriteLine("0. Quit");

            if (!int.TryParse(Console.ReadLine(), out userResp))
                userResp = -2;
            
            switch (userResp) {
                case 1:
                    EyeManager.ViewAllVision();
                    break;
                case 0:
                    Logging.Debug("Quitting server");
                    return;
            }
        }
    }

    public static async Task Test() {
        IPEndPoint point = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 8000);
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(point);
        socket.Listen();
        Socket handle = await socket.AcceptAsync();
        Stream dataStream = new NetworkStream(handle, true);
        await VLCLauncer.HostStream(dataStream, 60);
    } 
}