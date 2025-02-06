using Benzene.Abstractions.Results;

namespace Benzene.Extras.Results;

public class RawJsonMessage : IRawJsonMessage
{
    public RawJsonMessage(string json)
    {
        Json = json;
    }
    public string Json { get; }
}
