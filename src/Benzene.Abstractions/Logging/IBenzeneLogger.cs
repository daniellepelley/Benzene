namespace Benzene.Abstractions.Logging;

/// <summary>
/// Provides a logging abstraction for Benzene that is independent of any specific logging framework.
/// This interface enables provider-agnostic logging throughout the Benzene pipeline and middleware.
/// </summary>
public interface IBenzeneLogger
{
    /// <summary>
    /// Logs a message with the specified log level and optional exception.
    /// </summary>
    /// <param name="benzeneLogLevel">The severity level of the log entry.</param>
    /// <param name="exception">The exception to log, if any.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="args">Arguments to format into the message template.</param>
    void Log(BenzeneLogLevel benzeneLogLevel, Exception? exception, string message, params object[] args);
}
