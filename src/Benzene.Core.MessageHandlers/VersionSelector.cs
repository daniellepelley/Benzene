using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers;

public class VersionSelector : IVersionSelector
{
    public string Select(string requestedVersion, string[] availableVersions)
    {
        return availableVersions.Contains(requestedVersion)
            ? requestedVersion
            : availableVersions.MaxBy(x => x);
    }
}