using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
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
/// or left to cascade and fail the whole invocation.
/// </summary>
public class ServiceBusBatchApplication : IMiddlewareApplication<ServiceBusReceivedMessage[]>
{
    private readonly IMiddlewarePipeline<ServiceBusContext> _pipeline;
    private readonly ServiceBusOptions _options;

    public ServiceBusBatchApplication(IMiddlewarePipeline<ServiceBusContext> pipeline, ServiceBusOptions? options = null)
    {
        _pipeline = new TransportMiddlewarePipeline<ServiceBusContext>("service-bus", pipeline);
        _options = options ?? new ServiceBusOptions();
    }

    public async Task HandleAsync(ServiceBusReceivedMessage[] @event, IServiceResolverFactory serviceResolverFactory)
    {
        var tasks = @event.Select(message => new ServiceBusContext(message)).Select(async context =>
            {
                try
                {
                    using (var scope = serviceResolverFactory.CreateScope())
                    {
                        await _pipeline.HandleAsync(context, scope);
                    }

                    if (_options.RaiseOnFailureStatus && context.MessageResult?.IsSuccessful == false)
                    {
                        throw new ServiceBusMessageProcessingException(context.Message.MessageId);
                    }
                }
                catch (Exception ex) when (_options.CatchExceptions)
                {
                    using (var loggingScope = serviceResolverFactory.CreateScope())
                    {
                        loggingScope.GetService<ILogger<ServiceBusApplication>>()
                            .LogError(ex, "Processing Service Bus message {messageId} failed", context.Message.MessageId);
                    }
                }
            })
            .ToArray();

        await Task.WhenAll(tasks);
    }
}
