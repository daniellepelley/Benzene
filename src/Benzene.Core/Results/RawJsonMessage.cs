using Benzene.Abstractions.Results;

namespace Benzene.Core.Results;

public class RawJsonMessage : IRawJsonMessage
{
    public RawJsonMessage(string json)
    {
        Json = json;
    }
    public string Json { get; }
}
