namespace Benzene.Http.Routing;

/// <summary>
/// Provides a concrete implementation of <see cref="IHttpEndpointDefinition"/> that defines
/// the metadata for an HTTP endpoint.
/// </summary>
/// <remarks>
/// This class is used to register HTTP endpoints with their HTTP method, URL path pattern,
/// and associated topic (message name). Instances are typically created during endpoint
/// discovery and used by the routing system to match incoming requests to handlers.
/// </remarks>
public class HttpEndpointDefinition : IHttpEndpointDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpEndpointDefinition"/> class.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, DELETE, etc.).</param>
    /// <param name="path">The URL path pattern, which may include route parameters (e.g., "/users/{id}").</param>
    /// <param name="topic">The topic or message name that identifies the handler for this endpoint.</param>
    public HttpEndpointDefinition(string method, string path, string topic)
    {
        Method = method;
        Path = path;
        Topic = topic;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IHttpEndpointDefinition"/>.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, DELETE, etc.).</param>
    /// <param name="path">The URL path pattern, which may include route parameters (e.g., "/users/{id}").</param>
    /// <param name="topic">The topic or message name that identifies the handler for this endpoint.</param>
    /// <returns>A new HTTP endpoint definition.</returns>
    public static IHttpEndpointDefinition CreateInstance(string method, string path, string topic)
    {
        return new HttpEndpointDefinition(method, path, topic);
    }

    /// <summary>
    /// Gets the HTTP method (GET, POST, PUT, DELETE, etc.) for this endpoint.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Gets the URL path pattern for this endpoint, which may include route parameters (e.g., "/users/{id}").
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the topic or message name that identifies the handler for this endpoint.
    /// </summary>
    public string Topic { get; }
}
