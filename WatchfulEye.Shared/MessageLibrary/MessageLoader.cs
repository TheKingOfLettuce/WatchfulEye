using System.Reflection;
using System.Runtime.CompilerServices;
using LettuceTalk.Core;
using WatchfulEye.Shared.Utility;

namespace WatchfulEye.Shared.MessageLibrary;

public static class MessageLoader {
    [ModuleInitializer]
    internal static void AssociateMessages() {
        try {
            MessageFactory.AssociateAssembly(Assembly.GetAssembly(typeof(MessageLoader)));
        }
        catch (ArgumentException e) {
            Logging.Error("Failed to load messages", e);
            throw;
        }
    }
}