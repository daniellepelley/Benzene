using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandling;

namespace Benzene.Elements.Core.Broadcast;

public static class DependencyExtensions
{
    public static IMessageRouterBuilder UseBroadcastEvent(this IMessageRouterBuilder builder)
    {
        return builder.Add(new BroadcastEventMiddlewareBuilder());
    }
    
    public static IBenzeneServiceContainer AddBroadcastEvent(this IBenzeneServiceContainer builder, params IMessageDefinition[] messageDefinitions)
    {
        var broadcastEventChecker = new BroadcastEventChecker(messageDefinitions);
        builder.AddSingleton<IBroadcastEventChecker>(broadcastEventChecker);
        builder.AddSingleton<IMessageFinder<IMessageDefinition>>(broadcastEventChecker);
        return builder;
    }
}