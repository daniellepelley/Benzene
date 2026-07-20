using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core;
using Benzene.Grpc.Serialization;
using Benzene.Grpc.Streaming;
using Google.Protobuf;
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

        await RunPipelineAsync(grpcContext, context, resolver);

        if (grpcContext.Response is TResponse typed)
        {
            return typed;
        }

        return resolver.GetService<IGrpcMessageAdapter>().ConvertResponse<TResponse>(grpcContext.ResponsePayload);
    }

    public async Task ServerStreamingAsync<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context)
        where TRequest : class
        where TResponse : class
    {
        var grpcContext = new GrpcContext<TRequest, IAsyncEnumerable<TResponse>>(_grpcMethodDefinition.Topic, context, request);
        using var resolver = _serviceResolverFactory.CreateScope();

        await RunPipelineAsync(grpcContext, context, resolver);

        var items = ResolveResponseStream<TRequest, TResponse>(grpcContext, resolver, context.CancellationToken);
        await GrpcStreamAdapter.WriteAll(items, responseStream, context.CancellationToken);
    }

    public async Task<TResponse> ClientStreamingAsync<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context)
        where TRequest : class
        where TResponse : class
    {
        var requestItems = GrpcStreamAdapter.ReadAll(requestStream, context.CancellationToken);
        var grpcContext = new GrpcContext<IAsyncEnumerable<TRequest>, TResponse>(_grpcMethodDefinition.Topic, context, requestItems);
        using var resolver = _serviceResolverFactory.CreateScope();

        await RunPipelineAsync(grpcContext, context, resolver);

        if (grpcContext.Response is TResponse typed)
        {
            return typed;
        }

        return resolver.GetService<IGrpcMessageAdapter>().ConvertResponse<TResponse>(grpcContext.ResponsePayload);
    }

    public async Task DuplexStreamingAsync<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context)
        where TRequest : class
        where TResponse : class
    {
        var requestItems = GrpcStreamAdapter.ReadAll(requestStream, context.CancellationToken);
        var grpcContext = new GrpcContext<IAsyncEnumerable<TRequest>, IAsyncEnumerable<TResponse>>(_grpcMethodDefinition.Topic, context, requestItems);
        using var resolver = _serviceResolverFactory.CreateScope();

        await RunPipelineAsync(grpcContext, context, resolver);

        var items = ResolveResponseStream<IAsyncEnumerable<TRequest>, TResponse>(grpcContext, resolver, context.CancellationToken);
        await GrpcStreamAdapter.WriteAll(items, responseStream, context.CancellationToken);
    }

    /// <summary>
    /// Runs the middleware pipeline for one gRPC call, regardless of shape: populates the call accessor,
    /// translates a cancelled pipeline into the right <see cref="RpcException"/>, maps the handler's result
    /// status onto a trailer and (for non-OK results) an <see cref="RpcException"/>, and flushes any buffered
    /// response headers. Callers are responsible for extracting/converting the response (or response stream)
    /// from <paramref name="grpcContext"/> afterwards.
    /// </summary>
    private async Task RunPipelineAsync<TRequest, TResponse>(GrpcContext<TRequest, TResponse> grpcContext, ServerCallContext context, IServiceResolver resolver)
    {
        var callAccessor = resolver.TryGetService<GrpcServerCallAccessor>();
        if (callAccessor != null)
        {
            callAccessor.CallContext = context;
        }

        // Seed the scope's ambient cancellation token from the gRPC call's token (client cancel /
        // deadline), so a handler resolving ICancellationTokenAccessor observes it.
        resolver.SeedCancellationToken(context.CancellationToken);

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
            AddRichErrorDetails(grpcContext.ResponseTrailers, statusCode, detail, status, errors);
            throw new RpcException(new Status(statusCode, detail));
        }

        if (grpcContext.ResponseHeaders.Count > 0)
        {
            await context.WriteResponseHeadersAsync(grpcContext.ResponseHeaders);
        }
    }

    /// <summary>
    /// Attaches a <c>google.rpc.Status</c> to the <c>grpc-status-details-bin</c> trailer alongside the
    /// flat <c>benzene-status</c> trailer, so a gRPC client can read structured error details. A
    /// <see cref="Benzene.Results.BenzeneResultStatus.ValidationError"/> maps its error messages to a
    /// <c>google.rpc.BadRequest</c> with one field violation per message.
    /// </summary>
    private static void AddRichErrorDetails(Metadata trailers, StatusCode statusCode, string detail, string? status, string[]? errors)
    {
        var richStatus = new Google.Rpc.Status
        {
            Code = (int)statusCode,
            Message = detail,
        };

        if (status == Benzene.Results.BenzeneResultStatus.ValidationError && errors is { Length: > 0 })
        {
            var badRequest = new Google.Rpc.BadRequest();
            foreach (var error in errors)
            {
                badRequest.FieldViolations.Add(new Google.Rpc.BadRequest.Types.FieldViolation { Description = error });
            }

            richStatus.Details.Add(Google.Protobuf.WellKnownTypes.Any.Pack(badRequest));
        }

        // The "-bin" suffix marks a binary metadata value; the client reads it via GetRpcStatus().
        trailers.Add("grpc-status-details-bin", richStatus.ToByteArray());
    }

    private static IAsyncEnumerable<TResponseItem> ResolveResponseStream<TRequest, TResponseItem>(GrpcContext<TRequest, IAsyncEnumerable<TResponseItem>> grpcContext, IServiceResolver resolver, CancellationToken cancellationToken)
        where TResponseItem : class
    {
        if (grpcContext.Response != null)
        {
            return grpcContext.Response;
        }

        var adapter = resolver.GetService<IGrpcMessageAdapter>();
        if (GrpcStreamAdapter.TryConvertStream(grpcContext.ResponsePayload, typeof(IAsyncEnumerable<TResponseItem>), adapter, isResponseDirection: true, cancellationToken) is IAsyncEnumerable<TResponseItem> converted)
        {
            return converted;
        }

        throw new RpcException(new Status(StatusCode.Internal, "The message handler did not produce a response stream."));
    }
}
