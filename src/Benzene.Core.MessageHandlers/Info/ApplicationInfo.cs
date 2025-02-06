using Benzene.Abstractions.Info;

namespace Benzene.Core.MessageHandlers.Info;

public class ApplicationInfo : IApplicationInfo
{
    public ApplicationInfo(string name, string version, string description)
    {
        Name = name;
        Version = version;
        Description = description;
    }

    public string Name { get; }
    public string Version { get; }
    public string Description { get; }
}
