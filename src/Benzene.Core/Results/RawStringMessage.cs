using Benzene.Abstractions.Results;

namespace Benzene.Core.Results;

public class RawStringMessage : IRawStringMessage
{
    public RawStringMessage(string content)
    {
        Content = content;
    }

    public string Content { get; }
}
