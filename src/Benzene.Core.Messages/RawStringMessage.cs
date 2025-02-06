using Benzene.Abstractions.Messages;

namespace Benzene.Core.Messages;

public class RawStringMessage : IRawStringMessage
{
    public RawStringMessage(string content)
    {
        Content = content;
    }

    public string Content { get; }
}
