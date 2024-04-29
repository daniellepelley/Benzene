using Benzene.Core.BenzeneMessage;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Benzene.Grpc;

public class BenzeneInterceptor : Interceptor
{
    private readonly IGrpcMethodHandlerFactory _grpcMethodHandlerFactory;
    private readonly IGrpcRouteFinder _grpcRouteFinder;

    public BenzeneInterceptor(IGrpcMethodHandlerFactory grpcMethodHandlerFactory, IGrpcRouteFinder grpcRouteFinder)
    {
        _grpcRouteFinder = grpcRouteFinder;
        _grpcMethodHandlerFactory = grpcMethodHandlerFactory;
    }

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var topic = _grpcRouteFinder.Find(context.Method);

        if (topic != null)
        {
            var benzeneGrpcMethodHandler = _grpcMethodHandlerFactory.Create(topic);
            return base.UnaryServerHandler(request, context, benzeneGrpcMethodHandler.HandleAsync<TRequest, TResponse>);
        }

        return base.UnaryServerHandler(request, context, continuation);
    }
}