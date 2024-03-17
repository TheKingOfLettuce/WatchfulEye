// There is method chaining with the CallerInfo Attributes that ReSharper hates
// ReSharper disable ExplicitCallerInfoArgument

namespace WatchfulEye.Utility;

using System;
using Serilog;
using Serilog.Events;
using System.Runtime.CompilerServices;
using Serilog.Context;
using Serilog.Core;
using Serilog.Formatting.Json;

public static class Logging
{
    public const string LOG_TEMPLATE = "[{Timestamp:HH:mm:ss:fff} {Level}] ({File}/{Method}) {Message:j}{NewLine}{Exception}";
    public const string DEBUG_LOG_TEMPLATE = "[{Timestamp:HH:mm:ss:fff} {Level}] ({File}/{Method} at line {Line}) {Message:j}{NewLine}{Exception}";

    private static readonly LoggingLevelSwitch _logLevelSwitch = new LoggingLevelSwitch();

    public static ILogger LoggingInterface { get; private set; }

    public static LogEventLevel CurrentLevel => _logLevelSwitch.MinimumLevel;

    static Logging()
    {
        LoggerConfiguration logConfig = new LoggerConfiguration();
        
        string template;
        #if DEBUG
        template = DEBUG_LOG_TEMPLATE;
        #else
        template = LOG_TEMPLATE;
        #endif

        logConfig.WriteTo.Console(outputTemplate: template);
        logConfig.Enrich.FromLogContext();
        
        #if DEBUG
        _logLevelSwitch.MinimumLevel = LogEventLevel.Debug;
        #else
        _logLevelSwitch.MinimumLevel = LogEventLevel.Information;
        #endif

        logConfig.MinimumLevel.ControlledBy(_logLevelSwitch);
        LoggingInterface = logConfig.CreateLogger();
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
        (LoggingInterface as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Logs a <see cref="LogEventLevel.Verbose"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Verbose(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "",
        [CallerLineNumber]int lineNumber = -1)
        => LogMessage(LogEventLevel.Verbose, message, methodName, filePath, lineNumber);
    
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Verbose"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Verbose(string message, Exception? ex, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "",
        [CallerLineNumber]int lineNumber = -1)
        => LogMessage(LogEventLevel.Verbose, message, ex, methodName, filePath, lineNumber);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Debug"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Debug(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "",
        [CallerLineNumber]int lineNumber = -1)
        => LogMessage(LogEventLevel.Debug, message, methodName, filePath, lineNumber);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Debug"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Debug(string message, Exception? ex, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "",
        [CallerLineNumber]int lineNumber = -1)
        => LogMessage(LogEventLevel.Debug, message, ex, methodName, filePath, lineNumber);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Information"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Info(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "",
        [CallerLineNumber]int lineNumber = -1)
        => LogMessage(LogEventLevel.Information, message, methodName, filePath, lineNumber);

    /// <summary>
    /// Logs a <see cref="LogEventLevel.Information"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Info(string message, Exception? ex,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = -1)
        => LogMessage(LogEventLevel.Information, message, ex, methodName, filePath, lineNumber);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Warning"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Warning(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "",
        [CallerLineNumber]int lineNumber = -1)
        => LogMessage(LogEventLevel.Warning, message, methodName, filePath, lineNumber);

    /// <summary>
    /// Logs a <see cref="LogEventLevel.Warning"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Warning(string message, Exception? ex,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = -1)
        => LogMessage(LogEventLevel.Warning, message, ex, methodName, filePath, lineNumber);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Error"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Error(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "",
        [CallerLineNumber]int lineNumber = -1)
        => LogMessage(LogEventLevel.Error, message, methodName, filePath, lineNumber);

    /// <summary>
    /// Logs a <see cref="LogEventLevel.Error"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Error(string message, Exception? ex,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = -1)
        => LogMessage(LogEventLevel.Error, message, ex, methodName, filePath, lineNumber);
    
    /// <summary>
    /// Logs a <see cref="LogEventLevel.Fatal"/> message
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Fatal(string message, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "",
        [CallerLineNumber]int lineNumber = -1)
        => LogMessage(LogEventLevel.Fatal, message, methodName, filePath, lineNumber);

    /// <summary>
    /// Logs a <see cref="LogEventLevel.Fatal"/> message with an exception
    /// </summary>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void Fatal(string message, Exception? ex,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = -1)
        => LogMessage(LogEventLevel.Fatal, message, ex, methodName, filePath, lineNumber);

    /// <summary>
    /// Logs a message to the given <see cref="logLevel"/>
    /// </summary>
    /// <param name="logLevel">the <see cref="LogEventLevel"/> to log to</param>
    /// <param name="message">the message to log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void LogMessage(LogEventLevel logLevel, string message,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = -1)
        => LogMessage(logLevel, message, null, methodName, filePath, lineNumber);

    /// <summary>
    /// Logs a message and an exception to the given <see cref="logLevel"/>
    /// </summary>
    /// <param name="logLevel">the <see cref="LogEventLevel"/> to log to</param>
    /// <param name="message">the message to log</param>
    /// <param name="ex">the exception to also log</param>
    /// <param name="methodName">the calling method name, auto populated by <see cref="CallerMemberNameAttribute"/></param>
    /// <param name="filePath">the file the calling code came from, auto populated by <see cref="CallerFilePathAttribute"/></param>
    /// <param name="lineNumber">the line number from which the log came from, auto populated by <see cref="CallerLineNumberAttribute"/></param>
    public static void LogMessage(LogEventLevel logLevel, string message, Exception? ex, 
        [CallerMemberName]string methodName = "", 
        [CallerFilePath]string filePath = "",
        [CallerLineNumber]int lineNumber = -1)
    {
        using (LogContext.PushProperty("File", Path.GetFileNameWithoutExtension(filePath)))
        using (LogContext.PushProperty("Method", methodName))
        using (LogContext.PushProperty("Line", lineNumber))
        {
            LoggingInterface?.Write(logLevel, ex, message);
        }
    }
}
