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
        var topic = _grpcRouteFinder.Find(context.Method);

        if (topic != null)
        {
            var factory = _grpcMethodHandlerFactoryAccessor.Factory
                ?? throw new BenzeneException("No gRPC pipeline has been configured; call UseGrpc before handling requests.");
            var benzeneGrpcMethodHandler = factory.Create(topic);
            return base.UnaryServerHandler(request, context, benzeneGrpcMethodHandler.HandleAsync<TRequest, TResponse>);
        }

        return base.UnaryServerHandler(request, context, continuation);
    }
}
