namespace Benzene.Abstractions.MessageHandling;

public interface IVersionSelector
{
    string Select(string requestedVersion, string[] availableVersions);
}