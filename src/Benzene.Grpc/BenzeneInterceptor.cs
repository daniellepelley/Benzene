using Benzene.Core.Exceptions;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Benzene.Grpc;

public class BenzeneInterceptor : Interceptor
{
    private readonly IGrpcMethodHandlerFactoryAccessor _grpcMethodHandlerFactoryAccessor;
    private readonly IGrpcRouteFinder _grpcRouteFinder;

    public BenzeneInterceptor(IGrpcMethodHandlerFactoryAccessor grpcMethodHandlerFactoryAccessor, IGrpcRouteFinder grpcRouteFinder)
    {
        _grpcRouteFinder = grpcRouteFinder;
        _grpcMethodHandlerFactoryAccessor = grpcMethodHandlerFactoryAccessor;
    }

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var handler = TryCreateHandler(context);
        if (handler != null)
        {
            return base.UnaryServerHandler(request, context, handler.HandleAsync<TRequest, TResponse>);
        }

        return base.UnaryServerHandler(request, context, continuation);
    }

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var handler = TryCreateHandler(context);
        if (handler != null)
        {
            return base.ClientStreamingServerHandler(requestStream, context, handler.ClientStreamingAsync<TRequest, TResponse>);
        }

        return base.ClientStreamingServerHandler(requestStream, context, continuation);
    }

    public override Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var handler = TryCreateHandler(context);
        if (handler != null)
        {
            return base.ServerStreamingServerHandler(request, responseStream, context, handler.ServerStreamingAsync<TRequest, TResponse>);
        }

        return base.ServerStreamingServerHandler(request, responseStream, context, continuation);
    }

    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var handler = TryCreateHandler(context);
        if (handler != null)
        {
            return base.DuplexStreamingServerHandler(requestStream, responseStream, context, handler.DuplexStreamingAsync<TRequest, TResponse>);
        }

        return base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
    }

    private IGrpcMethodHandler? TryCreateHandler(ServerCallContext context)
    {
        var topic = _grpcRouteFinder.Find(context.Method);
        if (topic == null)
        {
            return null;
        }

        var factory = _grpcMethodHandlerFactoryAccessor.Factory
            ?? throw new BenzeneException("No gRPC pipeline has been configured; call UseGrpc before handling requests.");
        return factory.Create(topic);
    }
}
