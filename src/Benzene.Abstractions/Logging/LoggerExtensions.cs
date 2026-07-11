namespace Benzene.Abstractions.Logging;

/// <summary>
/// Provides convenience extension methods for IBenzeneLogger that simplify logging at specific severity levels.
/// These methods provide a fluent API similar to common logging frameworks.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs a debug message with an associated exception.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogDebug(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Debug, exception, message, args);
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogDebug(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Debug, message, args);
    }

    /// <summary>
    /// Logs a trace message with an associated exception.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogTrace(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Trace, exception, message, args);
    }

    /// <summary>
    /// Logs a trace message.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogTrace(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Trace, message, args);
    }

    /// <summary>
    /// Logs an informational message with an associated exception.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogInformation(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Information, exception, message, args);
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogInformation(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Information, message, args);
    }

    /// <summary>
    /// Logs a warning message with an associated exception.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogWarning(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Warning, exception, message, args);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogWarning(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Warning, message, args);
    }

    /// <summary>
    /// Logs an error message with an associated exception.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogError(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Error, exception, message, args);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogError(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Error, message, args);
    }

    /// <summary>
    /// Logs a critical message with an associated exception.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogCritical(this IBenzeneLogger benzeneLogger, Exception exception, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Critical, exception, message, args);
    }

    /// <summary>
    /// Logs a critical message.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void LogCritical(this IBenzeneLogger benzeneLogger, string message, params object[] args)
    {
        benzeneLogger.Log(BenzeneLogLevel.Critical, message, args);
    }

    /// <summary>
    /// Logs a message at the specified log level without an exception.
    /// </summary>
    /// <param name="benzeneLogger">The logger instance.</param>
    /// <param name="benzeneLogLevel">The severity level of the log entry.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    public static void Log(this IBenzeneLogger benzeneLogger, BenzeneLogLevel benzeneLogLevel, string message, params object[] args)
    {
        benzeneLogger.Log(benzeneLogLevel, null, message, args);
    }
}
