using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Grpc.Serialization;
using Benzene.Results;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Grpc.Client;

public class GrpcBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IGrpcMessageAdapter _adapter;
    private readonly IGrpcStatusReverseMapper _statusReverseMapper;
    private readonly ILogger<GrpcBenzeneMessageClient> _logger;
    private readonly IServiceResolver _serviceResolver;
    private readonly IMiddlewarePipeline<GrpcSendMessageContext> _middlewarePipeline;

    public GrpcBenzeneMessageClient(GrpcChannel channel, IGrpcClientRouteRegistry routeRegistry, IGrpcMessageAdapter adapter,
        IGrpcStatusReverseMapper statusReverseMapper, ILogger<GrpcBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _adapter = adapter;
        _statusReverseMapper = statusReverseMapper;
        _logger = logger;
        _serviceResolver = serviceResolver;

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<GrpcSendMessageContext>(benzeneServiceContainer);
        _middlewarePipeline = middlewarePipelineBuilder
            .UseGrpcClient(channel.CreateCallInvoker(), routeRegistry, adapter)
            .Build();
    }

    public GrpcBenzeneMessageClient(IMiddlewarePipeline<GrpcSendMessageContext> middlewarePipeline, IGrpcMessageAdapter adapter,
        IGrpcStatusReverseMapper statusReverseMapper, ILogger<GrpcBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _middlewarePipeline = middlewarePipeline;
        _adapter = adapter;
        _statusReverseMapper = statusReverseMapper;
        _logger = logger;
        _serviceResolver = serviceResolver;
    }

    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {
            // Propagate the ambient cancellation token (seeded by the gRPC server from the inbound
            // call's deadline/cancellation) onto the downstream RPC, so an upstream cancel aborts it
            // instead of leaving orphaned work running.
            var cancellationToken = _serviceResolver.TryGetService<ICancellationTokenAccessor>()?.CancellationToken ?? CancellationToken.None;
            var converter = new GrpcContextConverter<TRequest>(cancellationToken);
            var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));

            await _middlewarePipeline.HandleAsync(context, _serviceResolver);

            var status = _statusReverseMapper.Map(context.Status.StatusCode, context.ResponseTrailers);

            if (context.Status.StatusCode != StatusCode.OK)
            {
                var errors = string.IsNullOrEmpty(context.Status.Detail) ? Array.Empty<string>() : new[] { context.Status.Detail };
                return BenzeneResult.Set<TResponse>(status, errors);
            }

            var payload = _adapter.ConvertRequest<TResponse>(context.Response);
            return BenzeneResult.Set(status, payload!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message {topic} failed", request.Topic);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }

    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
