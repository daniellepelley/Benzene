using Google.Protobuf;

namespace Benzene.Grpc.Client;

public interface IGrpcClientRouteRegistry
{
    /// <summary>
    /// Registers a unary gRPC call under <paramref name="topic"/>. <typeparamref name="TRequest"/> and
    /// <typeparamref name="TResponse"/> are the RPC's protobuf wire types; <paramref name="fullMethodName"/>
    /// is the gRPC method's fully-qualified path, e.g. <c>/benzene.test.TestService/Echo</c>.
    /// </summary>
    IGrpcClientRouteRegistry Add<TRequest, TResponse>(string topic, string fullMethodName)
        where TRequest : class, IMessage<TRequest>
        where TResponse : class, IMessage<TResponse>;

    IGrpcClientRoute? Find(string topic);
}
