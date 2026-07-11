namespace Benzene.Http.Routing;

/// <summary>
/// Represents a matched HTTP route with its associated topic and extracted route parameters.
/// </summary>
/// <remarks>
/// This class is returned by the route finder when an HTTP request successfully matches a
/// configured endpoint. It contains the topic (message name) that identifies which handler
/// should process the request, and any route parameters extracted from the URL path
/// (e.g., the "id" value from a "/users/{id}" pattern).
/// </remarks>
public class HttpTopicRoute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpTopicRoute"/> class.
    /// </summary>
    /// <param name="topic">The topic or message name that identifies the handler for this route.</param>
    /// <param name="parameters">The route parameters extracted from the URL path.</param>
    public HttpTopicRoute(string topic, IDictionary<string, object> parameters)
    {
        Topic = topic;
        Parameters = parameters;
    }

    /// <summary>
    /// Gets the topic or message name that identifies which handler should process the request.
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// Gets the route parameters extracted from the URL path (e.g., "id" from "/users/{id}").
    /// </summary>
    public IDictionary<string, object> Parameters { get; }
}
