using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace WatchfulEye.Utility;

public static class Logging {
    private static ILogger _logger;
    private static LoggingLevelSwitch _switch;

    private const string LOG_TEMPLATE = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({Class}/{Method} as line {LineNum}) {Message}{NewLine}{Exception}";


    static Logging() {
        LoggerConfiguration config = new LoggerConfiguration();
        _switch = new LoggingLevelSwitch(LogEventLevel.Debug);
        config.MinimumLevel.ControlledBy(_switch);

        config.WriteTo.Console(outputTemplate: LOG_TEMPLATE);
        config.Enrich.FromLogContext();

        _logger = config.CreateLogger();

    }

    public static void Debug(string msg, 
    [CallerFilePath]string path = "", 
    [CallerMemberName]string method="", 
    [CallerLineNumber]int line = -1) => LogMessage(LogEventLevel.Debug, msg, path, method, line);

    public static void Info(string msg, 
    [CallerFilePath]string path = "", 
    [CallerMemberName]string method="", 
    [CallerLineNumber]int line = -1) => LogMessage(LogEventLevel.Information, msg, path, method, line);

    public static void Warning(string msg, 
    [CallerFilePath]string path = "", 
    [CallerMemberName]string method="", 
    [CallerLineNumber]int line = -1) => LogMessage(LogEventLevel.Warning, msg, path, method, line);

    public static void Error(string msg, 
    [CallerFilePath]string path = "", 
    [CallerMemberName]string method="", 
    [CallerLineNumber]int line = -1) => LogMessage(LogEventLevel.Error, msg, path, method, line);

    public static void Debug(string msg, Exception e,
    [CallerFilePath]string path = "", 
    [CallerMemberName]string method="", 
    [CallerLineNumber]int line = -1) => LogMessage(LogEventLevel.Debug, msg, e, path, method, line);

    public static void Info(string msg, Exception e,
    [CallerFilePath]string path = "", 
    [CallerMemberName]string method="", 
    [CallerLineNumber]int line = -1) => LogMessage(LogEventLevel.Information, msg, e, path, method, line);

    public static void Warning(string msg, Exception e, 
    [CallerFilePath]string path = "", 
    [CallerMemberName]string method="", 
    [CallerLineNumber]int line = -1) => LogMessage(LogEventLevel.Warning, msg, e, path, method, line);

    public static void Error(string msg, Exception e, 
    [CallerFilePath]string path = "", 
    [CallerMemberName]string method="", 
    [CallerLineNumber]int line = -1) => LogMessage(LogEventLevel.Error, msg, e, path, method, line);

    public static void LogMessage(LogEventLevel level, string msg, 
    [CallerFilePath]string path = "", 
    [CallerMemberName]string method="", 
    [CallerLineNumber]int line = -1) {
        using (LogContext.PushProperty("Class", Path.GetFileNameWithoutExtension(path))) {
            using (LogContext.PushProperty("Method", method)) {
                using (LogContext.PushProperty("LineNum", line)) {
                    _logger.Write(level, msg);
                }
            }
        }
    }

    public static void LogMessage(LogEventLevel level, string msg, Exception e,
    [CallerFilePath]string path = "", 
    [CallerMemberName]string method="", 
    [CallerLineNumber]int line = -1) {
        using (LogContext.PushProperty("Class", Path.GetFileNameWithoutExtension(path))) {
            using (LogContext.PushProperty("Method", method)) {
                using (LogContext.PushProperty("LineNum", line)) {
                    _logger.Write(level, msg, e);
                }
            }
        }
    }
}