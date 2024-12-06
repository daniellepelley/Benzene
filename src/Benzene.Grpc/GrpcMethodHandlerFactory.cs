using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.BenzeneMessage;
using Google.Protobuf.Reflection;

namespace Benzene.Grpc;

public class GrpcMethodHandlerFactory : IGrpcMethodHandlerFactory
{
    private readonly IBenzeneServiceContainer _services;
    private readonly ServiceDescriptor _serviceDescriptor;
    private readonly IMiddlewarePipeline<GrpcContext> _middlewarePipeline;

    public GrpcMethodHandlerFactory(IBenzeneServiceContainer services, ServiceDescriptor serviceDescriptor,
        IMiddlewarePipeline<GrpcContext> middlewarePipeline)
    {
        _serviceDescriptor = serviceDescriptor;
        _services = services;
        _middlewarePipeline = middlewarePipeline;
    }

    public IGrpcMethodHandler Create(IGrpcMethodDefinition grpcMethodDefinition)
    {
        return new GrpcMethodHandler(grpcMethodDefinition, _services.CreateServiceResolverFactory(), _middlewarePipeline, _serviceDescriptor);
    }
}

public class GrpcMethodHandlerFactory2 : IGrpcMethodHandlerFactory
{
    private readonly IBenzeneServiceContainer _services;
    private readonly ServiceDescriptor _serviceDescriptor;
    private readonly IMiddlewarePipeline<BenzeneMessageContext> _middlewarePipeline;

    public GrpcMethodHandlerFactory2(IBenzeneServiceContainer services, ServiceDescriptor serviceDescriptor,
        IMiddlewarePipeline<BenzeneMessageContext> middlewarePipeline)
    {
        _serviceDescriptor = serviceDescriptor;
        _services = services;
        _middlewarePipeline = middlewarePipeline;
    }

    public IGrpcMethodHandler Create(IGrpcMethodDefinition grpcMethodDefinition)
    {
        return new GrpcMethodHandler2(grpcMethodDefinition, _services.CreateServiceResolverFactory(), _middlewarePipeline, _serviceDescriptor);
    }
}


