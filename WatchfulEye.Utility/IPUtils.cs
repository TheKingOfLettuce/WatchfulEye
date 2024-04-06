using System.Net;
using System.Net.Sockets;

namespace WatchfulEye.Utility;

public static class IPUtils {

    /// <summary>
    /// Hack method to get the local IP of the device
    /// </summary>
    /// <returns>the local ip of the device</returns>
    public static string GetLocalIP() {
        using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
        socket.Connect("8.8.8.8", 65530);
        IPEndPoint point = socket.LocalEndPoint as IPEndPoint;
        return point.Address.ToString();
    }
}