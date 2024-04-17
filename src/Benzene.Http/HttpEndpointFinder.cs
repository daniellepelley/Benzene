using System.Reflection;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Exceptions;

namespace Benzene.Http;

public class HttpEndpointFinder : IHttpEndpointFinder
{
    private readonly IMessageHandlersFinder _messageHandlersFinder;

    public HttpEndpointFinder(IMessageHandlersFinder messageHandlersFinder)
    {
        _messageHandlersFinder = messageHandlersFinder;
    }

    public IHttpEndpointDefinition[] FindDefinitions()
    {
        var handlers = _messageHandlersFinder.FindDefinitions()
            .SelectMany(MapHandlers)
            .ToArray();

        var duplicates = handlers
            .GroupBy(x => new { x.Method, x.Path })
            .Where(x => x.Count() > 1)
            .ToArray();

        if (duplicates.Any())
        {
            throw new BenzeneException(
                $"Route '{duplicates[0].Key.Method} - {duplicates[0].Key.Path}' has been assigned to more than one message handler, this is not permitted");
        }

        return handlers;
    }

    private static IHttpEndpointDefinition[] MapHandlers(IMessageHandlerDefinition messageHandlerDefinition)
    {
        return messageHandlerDefinition.HandlerType.GetCustomAttributes<HttpEndpointAttribute>()
            .Select(httpEndpoint => MapEndpoint(httpEndpoint, messageHandlerDefinition.Topic))
            .Where(x => x != null)
            .Select(x => x!)
            .ToArray();
    }

    private static IHttpEndpointDefinition? MapEndpoint(HttpEndpointAttribute? httpEndpoint,
        string topic)
    {
        if (httpEndpoint == null)
        {
            return null;
        }

        return new HttpEndpointDefinition(httpEndpoint.Method, httpEndpoint.Url, topic);
    }
}
