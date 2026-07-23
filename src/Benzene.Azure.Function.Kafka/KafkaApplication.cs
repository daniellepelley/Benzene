using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Benzene.Azure.Function.Kafka;

/// <summary>
/// The entry point application for a Kafka-triggered Azure Function. Maps each event in the triggered
/// batch to a <see cref="KafkaContext"/> and runs them all through the middleware pipeline, tagging the
/// transport as <c>"kafka"</c> for the duration.
/// </summary>
public class KafkaApplication : EntryPointMiddlewareApplication<KafkaRecord[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Kafka middleware pipeline to run each event through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each batch.</param>
    /// <param name="options">
    /// Configures how a handler's exceptions and failure results are handled. Defaults to a new
    /// <see cref="KafkaOptions"/> instance (safe-by-default:
    /// <see cref="KafkaOptions.RaiseOnFailureStatus"/> on, <see cref="KafkaOptions.CatchExceptions"/>
    /// off) if omitted.
    /// </param>
    public KafkaApplication(IMiddlewarePipeline<KafkaContext> pipeline, IServiceResolverFactory serviceResolverFactory, KafkaOptions? options = null)
        : base(new KafkaBatchApplication(pipeline, options), serviceResolverFactory)
    { }
}

/// <summary>
/// Runs every record in a Kafka trigger batch through the middleware pipeline concurrently, each in
/// its own service scope, applying <see cref="KafkaOptions"/> to decide whether a record's exception
/// or failure result is contained (logged, doesn't affect the rest of the batch) or left to cascade
/// and fail the whole invocation.
/// </summary>
public class KafkaBatchApplication : IMiddlewareApplication<KafkaRecord[]>
{
    private readonly IMiddlewarePipeline<KafkaContext> _pipeline;
    private readonly KafkaOptions _options;

    public KafkaBatchApplication(IMiddlewarePipeline<KafkaContext> pipeline, KafkaOptions? options = null)
    {
        _pipeline = new TransportMiddlewarePipeline<KafkaContext>(TransportNames.Kafka, pipeline);
        _options = options ?? new KafkaOptions();
    }

    public async Task HandleAsync(KafkaRecord[] @event, IServiceResolverFactory serviceResolverFactory)
    {
        // BoundedFanOut optionally caps how many records run at once (KafkaOptions.MaxDegreeOfParallelism);
        // unset leaves the fan-out unbounded, exactly as before.
        var contexts = @event.Select(kafkaEvent => new KafkaContext(kafkaEvent));
        await BoundedFanOut.WhenAllAsync(contexts, async context =>
            {
                try
                {
                    using (var scope = serviceResolverFactory.CreateScope())
                    {
                        await _pipeline.HandleAsync(context, scope);
                    }

                    if (_options.RaiseOnFailureStatus && context.MessageResult?.IsSuccessful == false)
                    {
                        throw new KafkaMessageProcessingException(context.KafkaEvent.Topic);
                    }
                }
                catch (Exception ex) when (_options.CatchExceptions)
                {
                    using (var loggingScope = serviceResolverFactory.CreateScope())
                    {
                        loggingScope.GetService<ILogger<KafkaApplication>>()
                            .LogError(ex, "Processing Kafka record on topic {topic} failed", context.KafkaEvent.Topic);
                    }
                }
            }, _options.MaxDegreeOfParallelism);
    }
}
