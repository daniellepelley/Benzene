namespace Benzene.Http.Routing;

/// <summary>
/// Defines a contract for manually adding HTTP endpoint definitions to a list-based finder.
/// </summary>
/// <remarks>
/// This interface extends the endpoint finder pattern by allowing manual registration of
/// endpoints. It is useful for programmatically defining HTTP endpoints without using
/// attributes or reflection-based discovery.
/// </remarks>
public interface IListHttpEndpointFinder
{
    /// <summary>
    /// Adds an HTTP endpoint definition to the finder's list.
    /// </summary>
    /// <param name="httpEndpointDefinition">The HTTP endpoint definition to add.</param>
    void Add(IHttpEndpointDefinition httpEndpointDefinition);
}