namespace Benzene.Mesh.Contracts;

/// <summary>
/// The filter a <see cref="IMeshDiscoveryProvider"/> applies when enumerating services — which
/// resources count as Benzene services worth interrogating. Defaults to "carries the <c>benzene</c>
/// tag/label" but is fully user-overridable, so the mesh stays un-opinionated about a team's own tag
/// taxonomy.
/// </summary>
public class MeshDiscoveryFilter
{
    /// <summary>The default tag/label key a resource must carry to be discovered.</summary>
    public const string DefaultTagKey = "benzene";

    /// <summary>
    /// Initializes the filter.
    /// </summary>
    /// <param name="tags">
    /// Required tags/labels. A key must be present on the resource; a non-null value must match
    /// exactly (a null value means "present with any value"). Defaults to <c>{ "benzene": null }</c>.
    /// </param>
    /// <param name="regions">Optional region scoping (AWS/Azure). <c>null</c> = the provider's ambient/default region(s).</param>
    /// <param name="namespace">Optional namespace scoping (Kubernetes). <c>null</c> = provider default/all.</param>
    public MeshDiscoveryFilter(
        IReadOnlyDictionary<string, string?>? tags = null,
        IReadOnlyList<string>? regions = null,
        string? @namespace = null)
    {
        Tags = tags ?? new Dictionary<string, string?> { [DefaultTagKey] = null };
        Regions = regions;
        Namespace = @namespace;
    }

    /// <summary>The required tags/labels (key present; non-null value matched exactly).</summary>
    public IReadOnlyDictionary<string, string?> Tags { get; }

    /// <summary>Optional region scoping, or <c>null</c> for the provider's default.</summary>
    public IReadOnlyList<string>? Regions { get; }

    /// <summary>Optional namespace scoping (Kubernetes), or <c>null</c> for the provider's default.</summary>
    public string? Namespace { get; }

    /// <summary>
    /// Returns whether a resource carrying <paramref name="resourceTags"/> satisfies this filter —
    /// every required key must be present, and every required non-null value must match exactly.
    /// </summary>
    /// <param name="resourceTags">The resource's tags/labels.</param>
    public bool Matches(IReadOnlyDictionary<string, string> resourceTags)
    {
        foreach (var required in Tags)
        {
            if (!resourceTags.TryGetValue(required.Key, out var value))
            {
                return false;
            }

            if (required.Value != null && !string.Equals(required.Value, value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
