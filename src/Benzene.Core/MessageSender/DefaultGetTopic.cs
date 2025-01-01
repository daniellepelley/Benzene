using System;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.MessageSender;

public class DefaultGetTopic : IGetTopic
{
    public string GetTopic(Type type)
    {
        return string.Empty;
    }
}