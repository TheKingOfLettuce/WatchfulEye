using Serilog;
using Serilog.Events;
using System.Runtime.CompilerServices;
using Serilog.Context;
using Serilog.Core;

namespace WatchfulEye.Utility;

public static class Logging {
    public const string LOG_TEMPLATE = "{Level,11:u} {Timestamp:HH:mm:ss:fff} ({File}/{Method}) {Message:j}{NewLine}{Exception}";

    private static readonly LoggingLevelSwitch _logLevelSwitch = new LoggingLevelSwitch();

    public static ILogger LoggingInterface { get; private set; }

    public static LogEventLevel CurrentLevel => _logLevelSwitch.MinimumLevel;

    static Logging()
    {
        LoggerConfiguration logConfig = new LoggerConfiguration();

        logConfig.WriteTo.Console(outputTemplate: LOG_TEMPLATE);
        logConfig.WriteTo.File(Path.Combine(Directory.GetCurrentDirectory(), "Log.log"), outputTemplate: LOG_TEMPLATE, retainedFileCountLimit: 3, fileSizeLimitBytes: 5120000);
        logConfig.Enrich.FromLogContext();
        
        #if DEBUG
        _logLevelSwitch.MinimumLevel = LogEventLevel.Debug;
        #else
        _logLevelSwitch.MinimumLevel = LogEventLevel.Information;
        #endif

        logConfig.MinimumLevel.ControlledBy(_logLevelSwitch);
        LoggingInterface = logConfig.CreateLogger();

        Info($"{Environment.NewLine}{new string('-', 80)}");
        Info($"Logging configured for {_logLevelSwitch.MinimumLevel}");
    }

    /// <summary>
    /// Change the current logging level via a <see cref="LoggingLevelSwitch"/>
    /// </summary>
    /// <param name="newLevel">the new logging leve to switch to</param>
    public static void ChangeLogLevel(LogEventLevel newLevel) => _logLevelSwitch.MinimumLevel = newLevel;

    /// <summary>
    /// Closes logging, making sure to close any log files and flush queues
    /// </summary>
    public static void Close()
    {
        Info("Closing and flushing logs");
        (LoggingInterface as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Logs a <see cref="LogEventLevel.Verbose"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Verbose(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "")
        => LogMessage(LogEventLevel.Verbose, message, methodName, filePath);
    
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Verbose"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Verbose(string message, Exception ex, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "")
        => LogMessage(LogEventLevel.Verbose, message, ex, methodName, filePath);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Debug"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Debug(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "")
        => LogMessage(LogEventLevel.Debug, message, methodName, filePath);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Debug"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Debug(string message, Exception ex, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "")
        => LogMessage(LogEventLevel.Debug, message, ex, methodName, filePath);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Information"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Info(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "")
        => LogMessage(LogEventLevel.Information, message, methodName, filePath);

    /// <summary>
    /// Logs a <see cref="LogEventLevel.Information"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Info(string message, Exception ex,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
        => LogMessage(LogEventLevel.Information, message, ex, methodName, filePath);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Warning"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Warning(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "")
        => LogMessage(LogEventLevel.Warning, message, methodName, filePath);

    /// <summary>
    /// Logs a <see cref="LogEventLevel.Warning"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Warning(string message, Exception ex,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
        => LogMessage(LogEventLevel.Warning, message, ex, methodName, filePath);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Error"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Error(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "")
        => LogMessage(LogEventLevel.Error, message, methodName, filePath);

    /// <summary>
    /// Logs a <see cref="LogEventLevel.Error"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Error(string message, Exception ex,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
        => LogMessage(LogEventLevel.Error, message, ex, methodName, filePath);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Fatal"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Fatal(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "")
        => LogMessage(LogEventLevel.Fatal, message, methodName, filePath);

    /// <summary>
    /// Logs a <see cref="LogEventLevel.Fatal"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void Fatal(string message, Exception ex,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
        => LogMessage(LogEventLevel.Fatal, message, ex, methodName, filePath);

    /// <summary>
    /// Logs a message to the given <see cref="logLevel"/>
    /// </summary>
    /// <param name="logLevel">the <see cref="LogEventLevel"/> to log to</param>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void LogMessage(LogEventLevel logLevel, string message,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
        => LogMessage(logLevel, message, null, methodName, filePath);

    /// <summary>
    /// Logs a message and an exception to the given <see cref="logLevel"/>
    /// </summary>
    /// <param name="logLevel">the <see cref="LogEventLevel"/> to log to</param>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    public static void LogMessage(LogEventLevel logLevel, string message, Exception? ex, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "")
    {
        using (LogContext.PushProperty("File", Path.GetFileNameWithoutExtension(filePath)))
        using (LogContext.PushProperty("Method", methodName))
        {
            LoggingInterface.Write(logLevel, ex, message);
        }
    }
}