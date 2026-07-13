using System.Collections.Concurrent;
using System.Reflection;
using Benzene.Core.Exceptions;
using Google.Protobuf;
using Grpc.Core;

namespace Benzene.Grpc.Client;

public class GrpcClientRouteRegistry : IGrpcClientRouteRegistry
{
    private static readonly ConcurrentDictionary<Type, object> ParsersByType = new();

    private readonly ConcurrentDictionary<string, IGrpcClientRoute> _routesByTopic = new();

    public IGrpcClientRouteRegistry Add<TRequest, TResponse>(string topic, string fullMethodName)
        where TRequest : class, IMessage<TRequest>
        where TResponse : class, IMessage<TResponse>
    {
        var (serviceName, methodName) = SplitFullMethodName(fullMethodName);
        var method = new Method<TRequest, TResponse>(
            MethodType.Unary,
            serviceName,
            methodName,
            Marshallers.Create<TRequest>(m => m.ToByteArray(), bytes => ParserFor<TRequest>().ParseFrom(bytes)),
            Marshallers.Create<TResponse>(m => m.ToByteArray(), bytes => ParserFor<TResponse>().ParseFrom(bytes)));

        _routesByTopic[topic] = new GrpcClientRoute<TRequest, TResponse>(method);
        return this;
    }

    public IGrpcClientRoute? Find(string topic)
    {
        return _routesByTopic.TryGetValue(topic, out var route) ? route : null;
    }

    private static (string ServiceName, string MethodName) SplitFullMethodName(string fullMethodName)
    {
        var trimmed = fullMethodName.TrimStart('/');
        var separatorIndex = trimmed.LastIndexOf('/');
        if (separatorIndex < 0)
        {
            throw new BenzeneException($"'{fullMethodName}' is not a valid gRPC method name; expected '/package.Service/Method'.");
        }

        return (trimmed[..separatorIndex], trimmed[(separatorIndex + 1)..]);
    }

    private static MessageParser<T> ParserFor<T>() where T : IMessage<T>
    {
        return (MessageParser<T>)ParsersByType.GetOrAdd(typeof(T), static t =>
        {
            var property = t.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
            return property?.GetValue(null)
                ?? throw new BenzeneException($"Type {t.Name} is not a protobuf message; it does not expose a static Parser property.");
        });
    }
}
