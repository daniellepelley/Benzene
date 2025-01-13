using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;

namespace Benzene.Core.Results;

public class RawStringMessage : IRawStringMessage
{
    public RawStringMessage(string content)
    {
        Content = content;
    }

    public string Content { get; }
}
