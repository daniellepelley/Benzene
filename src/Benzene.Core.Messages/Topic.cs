using Benzene.Abstractions.Messages;

namespace Benzene.Core.Messages;

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
