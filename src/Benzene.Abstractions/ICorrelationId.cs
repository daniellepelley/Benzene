namespace Benzene.Abstractions;

/// <summary>
/// Provides access to the correlation ID for the current request.
/// Correlation IDs enable distributed tracing across service boundaries and log aggregation.
/// </summary>
public interface ICorrelationId
{
    /// <summary>
    /// Sets the correlation ID for the current request.
    /// </summary>
    /// <param name="correlationId">The correlation ID to set.</param>
    void Set(string correlationId);

    /// <summary>
    /// Gets the correlation ID for the current request.
    /// </summary>
    /// <returns>The current correlation ID.</returns>
    string Get();
}