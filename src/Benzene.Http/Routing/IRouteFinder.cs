namespace Benzene.Http.Routing;

/// <summary>
/// Defines a contract for finding HTTP routes that match a given HTTP method and path.
/// </summary>
/// <remarks>
/// The route finder is responsible for matching incoming HTTP requests to configured endpoints
/// by comparing the request's HTTP method and URL path against registered route patterns.
/// It supports route parameters (e.g., "/users/{id}") and returns the matched route with
/// extracted parameter values.
/// </remarks>
public interface IRouteFinder
{
    /// <summary>
    /// Finds an HTTP route that matches the specified HTTP method and path.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, DELETE, etc.) to match.</param>
    /// <param name="path">The URL path to match against registered route patterns.</param>
    /// <returns>
    /// An <see cref="HttpTopicRoute"/> containing the matched route and extracted parameters,
    /// or <c>null</c> if no matching route is found.
    /// </returns>
    HttpTopicRoute? Find(string method, string path);
}