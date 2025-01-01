using System;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.MessageSender;

public static class MessageSenderExtensions
{
    public static IRegisterDependency Out(this IRegisterDependency source, Action<IMessageSenderBuilder> action)
    {
        var messageSenderBuilder = new MessageSenderBuilder(source);
        action(messageSenderBuilder);
        return source;
    }
}