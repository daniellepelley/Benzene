using Benzene.Abstractions.Messages.BenzeneClient;

namespace Benzene.Core.Messages.MessageSender;

public class DefaultGetTopic : IGetTopic
{
    public string GetTopic(Type type)
    {
        return string.Empty;
    }
}