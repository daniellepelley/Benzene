namespace Benzene.Abstractions.Logging;

/// <summary>
/// Provides an interface for appending log entries to a logging provider.
/// This interface abstracts the underlying logging implementation, enabling provider-agnostic logging in Benzene.
/// </summary>
public interface IBenzeneLogAppender
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
