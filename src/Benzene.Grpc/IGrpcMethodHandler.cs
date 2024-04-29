using Grpc.Core;

namespace Benzene.Grpc;

public interface IGrpcMethodHandler
{
    Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request, ServerCallContext context)
        where TRequest : class
        where TResponse : class;
}