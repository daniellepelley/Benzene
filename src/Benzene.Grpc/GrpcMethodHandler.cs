using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Grpc.Serialization;
using Grpc.Core;

namespace Benzene.Grpc;

public class GrpcMethodHandler : IGrpcMethodHandler
{
    private IGrpcMethodDefinition _grpcMethodDefinition;
    private IServiceResolverFactory _serviceResolverFactory;
    private IMiddlewarePipeline<GrpcContext> _middlewarePipeline;

    public GrpcMethodHandler(IGrpcMethodDefinition grpcMethodDefinition, IServiceResolverFactory serviceResolverFactory, IMiddlewarePipeline<GrpcContext> middlewarePipeline)
    {
        _middlewarePipeline = middlewarePipeline;
        _serviceResolverFactory = serviceResolverFactory;
        _grpcMethodDefinition = grpcMethodDefinition;
    }

    public async Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request, ServerCallContext context)
        where TRequest : class
        where TResponse : class
    {
        var grpcContext = new GrpcContext<TRequest, TResponse>(_grpcMethodDefinition.Topic, request);

        using var resolver = _serviceResolverFactory.CreateScope();
        await _middlewarePipeline.HandleAsync(grpcContext, resolver);

        if (grpcContext.Response is TResponse typed)
        {
            return typed;
        }

        return resolver.GetService<IGrpcMessageAdapter>().ConvertResponse<TResponse>(grpcContext.ResponsePayload);
    }
}
