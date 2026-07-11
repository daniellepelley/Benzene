namespace Benzene.Abstractions;

/// <summary>
/// Provides a fluent builder interface for constructing HTTP requests in test scenarios.
/// This interface enables HTTP-based testing of Benzene handlers and middleware.
/// </summary>
/// <typeparam name="T">The type of the message body.</typeparam>
public interface IHttpBuilder<T>
{
    /// <summary>
    /// Gets the HTTP headers for the request.
    /// </summary>
    IDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets the HTTP method (GET, POST, PUT, DELETE, etc.).
    /// </summary>
    string Method { get; }

    /// <summary>
    /// Gets the request path.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Gets the message body.
    /// </summary>
    T? Message { get; }
}
