using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;

namespace Benzene.Extras.Broadcast;

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
        builder.AddSingleton<IMessageDefinitionFinder<IMessageDefinition>>(broadcastEventChecker);
        return builder;
    }
}