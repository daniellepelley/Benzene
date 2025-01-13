using System.Linq;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Core.MessageHandling;

public class VersionSelector : IVersionSelector
{
    public string Select(string requestedVersion, string[] availableVersions)
    {
        return availableVersions.Contains(requestedVersion)
            ? requestedVersion
            : availableVersions.MaxBy(x => x);
    }
}