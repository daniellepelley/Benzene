namespace Benzene.Abstractions.Logging;

/// <summary>
/// Provides a mechanism for creating scoped log contexts with structured properties.
/// Log contexts enable structured logging with key-value pairs that are automatically included in all log entries within the scope.
/// </summary>
public interface IBenzeneLogContext
{
    /// <summary>
    /// Creates a new log context scope with the specified properties.
    /// All log entries written within the returned disposable scope will include these properties.
    /// </summary>
    /// <param name="properties">The key-value pairs to include in the log context.</param>
    /// <returns>A disposable scope that removes the properties when disposed.</returns>
    IDisposable Create(IDictionary<string, string> properties);
}
