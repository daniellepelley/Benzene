namespace Benzene.Http.Routing;

/// <summary>
/// Defines the metadata for an HTTP endpoint, including its HTTP method, URL path, and associated topic.
/// </summary>
/// <remarks>
/// HTTP endpoint definitions are used to map incoming HTTP requests to message handlers.
/// The topic identifies which message handler should process requests matching the method and path.
/// </remarks>
public interface IHttpEndpointDefinition
{
    /// <summary>
    /// Gets the HTTP method (GET, POST, PUT, DELETE, etc.) for this endpoint.
    /// </summary>
    string Method { get; }

    /// <summary>
    /// Gets the URL path pattern for this endpoint, which may include route parameters (e.g., "/users/{id}").
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Gets the topic or message name that identifies the handler for this endpoint.
    /// </summary>
    string Topic { get; }
}