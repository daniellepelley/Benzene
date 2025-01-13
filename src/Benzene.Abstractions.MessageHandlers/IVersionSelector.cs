namespace Benzene.Abstractions.MessageHandlers;

public interface IVersionSelector
{
    string Select(string requestedVersion, string[] availableVersions);
}