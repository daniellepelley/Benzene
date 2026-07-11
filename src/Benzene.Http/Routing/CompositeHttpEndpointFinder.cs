namespace Benzene.Http.Routing;

/// <summary>
/// Combines multiple <see cref="IHttpEndpointFinder"/> instances into a single finder that aggregates
/// their results.
/// </summary>
/// <remarks>
/// This finder allows combining different endpoint discovery strategies (reflection, manual registration,
/// dependency injection, etc.) into a unified endpoint discovery mechanism. It calls all inner finders
/// and merges their results into a single array of endpoint definitions.
/// </remarks>
public class CompositeHttpEndpointFinder : IHttpEndpointFinder
{
    private readonly IHttpEndpointFinder[] _inners;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeHttpEndpointFinder"/> class.
    /// </summary>
    /// <param name="inners">The endpoint finders to combine.</param>
    public CompositeHttpEndpointFinder(params IHttpEndpointFinder[] inners)
    {
        _inners = inners;
    }

    /// <summary>
    /// Finds and returns all HTTP endpoint definitions from all inner finders.
    /// </summary>
    /// <returns>
    /// An array containing the combined endpoint definitions from all inner finders.
    /// </returns>
    public IHttpEndpointDefinition[] FindDefinitions()
    {
        return _inners.SelectMany(x => x.FindDefinitions()).ToArray();
    }
}
