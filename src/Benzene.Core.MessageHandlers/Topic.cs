using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.MessageHandlers;

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
