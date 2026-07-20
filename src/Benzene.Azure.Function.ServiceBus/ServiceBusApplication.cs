using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Benzene.Azure.Function.ServiceBus;

/// <summary>
/// The entry point application for a Service Bus-triggered Azure Function. Maps each message in the
/// triggered batch to a <see cref="ServiceBusContext"/> and runs them all through the middleware pipeline,
/// tagging the transport as <c>"service-bus"</c> for the duration.
/// </summary>
public class ServiceBusApplication : EntryPointMiddlewareApplication<ServiceBusReceivedMessage[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Service Bus middleware pipeline to run each message through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each batch.</param>
    /// <param name="options">
    /// Configures how a handler's exceptions and failure results are handled. Defaults to a new
    /// <see cref="ServiceBusOptions"/> instance (both <see cref="ServiceBusOptions.CatchExceptions"/>
    /// and <see cref="ServiceBusOptions.RaiseOnFailureStatus"/> off) if omitted.
    /// </param>
    public ServiceBusApplication(IMiddlewarePipeline<ServiceBusContext> pipeline, IServiceResolverFactory serviceResolverFactory, ServiceBusOptions? options = null)
        : base(new ServiceBusBatchApplication(pipeline, options), serviceResolverFactory)
    { }
}

/// <summary>
/// Runs every message in a Service Bus trigger batch through the middleware pipeline concurrently,
/// each in its own service scope, applying <see cref="ServiceBusOptions"/> to decide whether a
/// message's exception or failure result is contained (logged, doesn't affect the rest of the batch)
/// or left to cascade and fail the whole invocation - and, when <see cref="ServiceBusOptions.AckMode"/>
/// is <see cref="ServiceBusAckMode.Explicit"/>, to complete or abandon each message individually
/// based on that same outcome.
/// </summary>
public class ServiceBusBatchApplication : IMiddlewareApplication<ServiceBusReceivedMessage[]>, IMiddlewareApplication<ServiceBusTriggerBatch>
{
    private readonly IMiddlewarePipeline<ServiceBusContext> _pipeline;
    private readonly ServiceBusOptions _options;

    public ServiceBusBatchApplication(IMiddlewarePipeline<ServiceBusContext> pipeline, ServiceBusOptions? options = null)
    {
        _pipeline = new TransportMiddlewarePipeline<ServiceBusContext>(TransportNames.ServiceBus, pipeline);
        _options = options ?? new ServiceBusOptions();
    }

    /// <summary>
    /// Handles a batch with no <see cref="ServiceBusMessageActions"/> available - <see cref="ServiceBusOptions.AckMode"/>
    /// has no effect here even if set to <see cref="ServiceBusAckMode.Explicit"/>, since there is
    /// nothing to complete/abandon against; use the <see cref="ServiceBusTriggerBatch"/> overload
    /// (via <c>Extensions.HandleServiceBusMessages(IAzureFunctionApp, ServiceBusMessageActions,
    /// ServiceBusReceivedMessage[])</c>) for explicit ack mode to take effect.
    /// </summary>
    public Task HandleAsync(ServiceBusReceivedMessage[] @event, IServiceResolverFactory serviceResolverFactory)
        => HandleAsync(@event, messageActions: null, serviceResolverFactory);

    /// <summary>
    /// Handles a batch together with the <see cref="ServiceBusMessageActions"/> needed to complete/abandon
    /// each message - required for <see cref="ServiceBusOptions.AckMode"/> = <see cref="ServiceBusAckMode.Explicit"/>
    /// to actually complete/abandon messages.
    /// </summary>
    public Task HandleAsync(ServiceBusTriggerBatch @event, IServiceResolverFactory serviceResolverFactory)
        => HandleAsync(@event.Messages, @event.MessageActions, serviceResolverFactory);

    private async Task HandleAsync(ServiceBusReceivedMessage[] messages, ServiceBusMessageActions? messageActions, IServiceResolverFactory serviceResolverFactory)
    {
        var explicitAck = messageActions != null && _options.AckMode == ServiceBusAckMode.Explicit;

        // BoundedFanOut optionally caps how many messages run at once
        // (ServiceBusOptions.MaxDegreeOfParallelism); unset leaves the fan-out unbounded, exactly as
        // before.
        var contexts = messages.Select(message => new ServiceBusContext(message));
        await BoundedFanOut.WhenAllAsync(contexts, async context =>
            {
                var acked = false;

                try
                {
                    using (var scope = serviceResolverFactory.CreateScope())
                    {
                        await _pipeline.HandleAsync(context, scope);
                    }

                    if (explicitAck)
                    {
                        acked = true;
                        if (context.MessageResult?.IsSuccessful == false)
                        {
                            await messageActions!.AbandonMessageAsync(context.Message);
                        }
                        else
                        {
                            await messageActions!.CompleteMessageAsync(context.Message);
                        }
                    }

                    if (_options.RaiseOnFailureStatus && context.MessageResult?.IsSuccessful == false)
                    {
                        throw new ServiceBusMessageProcessingException(context.Message.MessageId);
                    }
                }
                catch (Exception ex) when (_options.CatchExceptions)
                {
                    if (explicitAck && !acked)
                    {
                        await messageActions!.AbandonMessageAsync(context.Message);
                    }

                    using (var loggingScope = serviceResolverFactory.CreateScope())
                    {
                        loggingScope.GetService<ILogger<ServiceBusApplication>>()
                            .LogError(ex, "Processing Service Bus message {messageId} failed", context.Message.MessageId);
                    }
                }
                catch (Exception) when (explicitAck && !acked)
                {
                    await messageActions!.AbandonMessageAsync(context.Message);
                    throw;
                }
            }, _options.MaxDegreeOfParallelism);
    }
}
