﻿using WatchfulEye.Client.Eyes;
using WatchfulEye.Utility;

namespace WatchfulEye.Client;

internal static class Program {
    public static async Task Main(string[] args) {
        Logging.Debug("Creating eyeball");
        using EyeBall? eye = EyeBall.SocketEye(args[0]);
        if (eye == null) {
            Logging.Error("Could not socket eye");
            return;
        }
        eye.DisconnectedWaiter.WaitOne();
        Logging.Debug("Shutting program down");
    }

    public static async Task Block() {
        Console.WriteLine("Press any key to quit");
        Console.Read();
        return;
    }
}




