namespace Benzene.Http.Routing;

/// <summary>
/// Defines a contract for discovering HTTP endpoint definitions in the application.
/// </summary>
/// <remarks>
/// Implementations of this interface provide different strategies for finding HTTP endpoints,
/// such as reflection-based discovery, dependency injection container scanning, or manual
/// registration. Multiple finders can be composed to support different discovery mechanisms.
/// </remarks>
public interface IHttpEndpointFinder
{
    /// <summary>
    /// Finds and returns all HTTP endpoint definitions.
    /// </summary>
    /// <returns>An array of HTTP endpoint definitions discovered by this finder.</returns>
    IHttpEndpointDefinition[] FindDefinitions();
}