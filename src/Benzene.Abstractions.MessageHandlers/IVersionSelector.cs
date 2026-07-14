namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Chooses which handler version to route a message to when a topic has multiple registered
/// handler versions, allowing several versions of the same message contract to coexist (e.g. so
/// callers can migrate at their own pace). The default implementation matches the requested version
/// exactly if available, otherwise falls back to the highest available version.
/// </summary>
public interface IVersionSelector
{
    /// <summary>Selects a handler version to use for the requested version.</summary>
    /// <param name="requestedVersion">The version requested by the incoming message.</param>
    /// <param name="availableVersions">The versions of the handler that are registered for the topic.</param>
    /// <returns>The version to route to, which must be one of <paramref name="availableVersions"/>.</returns>
    string Select(string requestedVersion, string[] availableVersions);
}