namespace Benzene.Http;

/// <summary>
/// Marks a message handler class as an HTTP endpoint with a specific HTTP method and URL pattern.
/// </summary>
/// <remarks>
/// This attribute can be applied multiple times to the same class to define multiple HTTP endpoints
/// that route to the same handler. It is used by reflection-based endpoint discovery to automatically
/// register HTTP routes. The URL pattern can include route parameters (e.g., "/users/{id}").
/// The handler must also carry a <c>[Message("topic")]</c> attribute — handler discovery skips
/// classes without one, so an <see cref="HttpEndpointAttribute"/> on its own registers no route.
/// </remarks>
/// <example>
/// <code>
/// [Message("users:get")]
/// [HttpEndpoint("GET", "/users/{id}")]
/// [HttpEndpoint("GET", "/api/users/{id}")]
/// public class GetUserHandler : IMessageHandler&lt;GetUserRequest, GetUserResponse&gt;
/// {
///     // Handler implementation
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class HttpEndpointAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpEndpointAttribute"/> class.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, DELETE, PATCH, etc.).</param>
    /// <param name="url">The URL pattern for the endpoint, which may include route parameters (e.g., "/users/{id}").</param>
    public HttpEndpointAttribute(string method, string url)
    {
        Url = url;
        Method = method;
    }

    /// <summary>
    /// Gets the HTTP method for this endpoint.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Gets the URL pattern for this endpoint, which may include route parameters.
    /// </summary>
    public string Url { get; }
}