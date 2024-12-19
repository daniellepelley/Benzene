using System.Reflection;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Exceptions;

namespace Benzene.Grpc;

public class ReflectionGrpcMethodFinder : IGrpcMethodFinder
{
    private readonly IMessageHandlersFinder _messageHandlersFinder;

    public ReflectionGrpcMethodFinder(IMessageHandlersFinder messageHandlersFinder)
    {
        _messageHandlersFinder = messageHandlersFinder;
    }

    public IGrpcMethodDefinition[] FindDefinitions()
    {
        var handlers = _messageHandlersFinder.FindDefinitions()
            .SelectMany(MapHandlers)
            .ToArray();

        var duplicates = handlers
            .GroupBy(x => new { x.Method})
            .Where(x => x.Count() > 1)
            .ToArray();

        if (duplicates.Any())
        {
            throw new BenzeneException(
                $"Grpc method '{duplicates[0].Key.Method}' has been assigned to more than one message handler, this is not permitted");
        }

        return handlers;
    }

    private static IGrpcMethodDefinition[] MapHandlers(IMessageHandlerDefinition messageHandlerDefinition)
    {
        return messageHandlerDefinition.HandlerType.GetCustomAttributes<GrpcMethodAttribute>()
            .Select(GrpcMethod => MapEndpoint(GrpcMethod, messageHandlerDefinition.Topic))
            .Where(x => x != null)
            .Select(x => x!)
            .ToArray();
    }

    private static IGrpcMethodDefinition? MapEndpoint(GrpcMethodAttribute? GrpcMethod,
        string topic)
    {
        if (GrpcMethod == null)
        {
            return null;
        }

        return new GrpcMethodDefinition(GrpcMethod.Method, topic);
    }
}
