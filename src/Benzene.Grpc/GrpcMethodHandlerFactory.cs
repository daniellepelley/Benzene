using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Grpc;

public class GrpcMethodHandlerFactory : IGrpcMethodHandlerFactory
{
    private readonly IBenzeneServiceContainer _services;
    private readonly IMiddlewarePipeline<GrpcContext> _middlewarePipeline;

    public GrpcMethodHandlerFactory(IBenzeneServiceContainer services, IMiddlewarePipeline<GrpcContext> middlewarePipeline)
    {
        _services = services;
        _middlewarePipeline = middlewarePipeline;
    }

    public IGrpcMethodHandler Create(IGrpcMethodDefinition grpcMethodDefinition)
    {
        return new GrpcMethodHandler(grpcMethodDefinition, _services.CreateServiceResolverFactory(), _middlewarePipeline);
    }
}
