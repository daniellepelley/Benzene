using System;
using Benzene.Abstractions.Middleware.BenzeneClient;

namespace Benzene.Core.MessageSender;

public class DefaultGetTopic : IGetTopic
{
    public string GetTopic(Type type)
    {
        return string.Empty;
    }
}