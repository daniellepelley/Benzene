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
        var grpcContext = new GrpcContext<TRequest, TResponse>(_grpcMethodDefinition.Topic, context, request);

        using var resolver = _serviceResolverFactory.CreateScope();

        var callAccessor = resolver.TryGetService<GrpcServerCallAccessor>();
        if (callAccessor != null)
        {
            callAccessor.CallContext = context;
        }

        try
        {
            await _middlewarePipeline.HandleAsync(grpcContext, resolver);
        }
        catch (OperationCanceledException)
        {
            var cancelCode = DateTime.UtcNow >= context.Deadline ? StatusCode.DeadlineExceeded : StatusCode.Cancelled;
            throw new RpcException(new Status(cancelCode, "The call was cancelled."));
        }

        var status = grpcContext.MessageHandlerResult?.BenzeneResult.Status;
        grpcContext.ResponseTrailers.Add("benzene-status", status ?? "Unknown");

        var statusCode = resolver.GetService<IGrpcStatusCodeMapper>().Map(status);
        if (statusCode != StatusCode.OK)
        {
            var errors = grpcContext.MessageHandlerResult?.BenzeneResult.Errors;
            var detail = errors is { Length: > 0 } ? string.Join("; ", errors) : status ?? "Error";
            throw new RpcException(new Status(statusCode, detail));
        }

        if (grpcContext.ResponseHeaders.Count > 0)
        {
            await context.WriteResponseHeadersAsync(grpcContext.ResponseHeaders);
        }

        if (grpcContext.Response is TResponse typed)
        {
            return typed;
        }

        return resolver.GetService<IGrpcMessageAdapter>().ConvertResponse<TResponse>(grpcContext.ResponsePayload);
    }
}
