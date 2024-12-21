using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers;

public class Topic : ITopic 
{
    public Topic(string id)
    {
        Id = id ?? Constants.Missing.Id;
        Version = string.Empty;
    }

    public Topic(string id, string version)
        :this(id)
    {
        Version = version ?? string.Empty;
    }

    public string Id { get; }
    public string Version { get; }
}
