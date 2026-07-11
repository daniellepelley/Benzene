namespace Benzene.Abstractions.Logging;

/// <summary>
/// Defines the severity levels for logging in Benzene.
/// These levels align with common logging frameworks to enable consistent logging across providers.
/// </summary>
public enum BenzeneLogLevel
{
    /// <summary>
    /// Logs that contain the most detailed messages. These messages may contain sensitive application data and should not be enabled in production.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Logs used for interactive investigation during development. These logs primarily contain information useful for debugging.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Logs that track the general flow of the application. These logs typically have long-term value.
    /// </summary>
    Information = 2,

    /// <summary>
    /// Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the application execution to stop.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Logs that highlight when the current flow of execution is stopped due to a failure.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Logs that describe an unrecoverable application or system crash, or a catastrophic failure that requires immediate attention.
    /// </summary>
    Critical = 5,

    /// <summary>
    /// Not used for writing log messages. Specifies that logging is disabled.
    /// </summary>
    None = 6,
}