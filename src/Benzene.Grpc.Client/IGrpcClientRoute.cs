using Benzene.Grpc.Serialization;
using Grpc.Core;

namespace Benzene.Grpc.Client;

/// <summary>
/// A registered gRPC client call, closed over its request/response protobuf wire types. Bridges the
/// topic-addressed, boxed-payload world of <see cref="GrpcSendMessageContext"/> to a strongly-typed
/// <see cref="Method{TRequest,TResponse}"/> invocation via <see cref="CallInvoker"/>.
/// </summary>
public interface IGrpcClientRoute
{
    Task InvokeAsync(CallInvoker invoker, IGrpcMessageAdapter adapter, GrpcSendMessageContext context);
}
