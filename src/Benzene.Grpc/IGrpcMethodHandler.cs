using Grpc.Core;

namespace Benzene.Grpc;

public interface IGrpcMethodHandler
{
    Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request, ServerCallContext context)
        where TRequest : class
        where TResponse : class;

    Task ServerStreamingAsync<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context)
        where TRequest : class
        where TResponse : class;

    Task<TResponse> ClientStreamingAsync<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context)
        where TRequest : class
        where TResponse : class;

    Task DuplexStreamingAsync<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context)
        where TRequest : class
        where TResponse : class;
}
