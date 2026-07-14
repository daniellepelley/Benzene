using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IVersionSelector"/> implementation: uses the requested version if a handler
/// registered exactly that version exists, otherwise falls back to the highest available version
/// (by ordinal string comparison).
/// </summary>
public class VersionSelector : IVersionSelector
{
    /// <summary>
    /// Selects which of the available handler versions to use for a request.
    /// </summary>
    /// <param name="requestedVersion">The version requested by the incoming message (may be empty/unversioned).</param>
    /// <param name="availableVersions">The versions registered for the topic.</param>
    /// <returns>
    /// <paramref name="requestedVersion"/> if it is present in <paramref name="availableVersions"/>;
    /// otherwise the maximum value in <paramref name="availableVersions"/> by ordinal string comparison.
    /// </returns>
    public string Select(string requestedVersion, string[] availableVersions)
    {
        return availableVersions.Contains(requestedVersion)
            ? requestedVersion
            : availableVersions.MaxBy(x => x);
    }
}
