using Benzene.Abstractions.MessageHandling;

namespace Benzene.Core.Mappers;

public class Topic : ITopic 
{
    public Topic(string id)
    {
        Id = id ?? Constants.Missing;
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
