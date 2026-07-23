using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// The entry point application for a Queue Storage-triggered Azure Function. Maps each message to a
/// <see cref="QueueStorageContext"/> and runs it through the middleware pipeline, tagging the
/// transport as <c>"queue-storage"</c> for the duration. Exception/failure-status behavior is
/// configurable via <see cref="QueueStorageOptions"/>, mirroring <c>Benzene.Azure.Function.Kafka</c>.
/// </summary>
public class QueueStorageApplication : EntryPointMiddlewareApplication<QueueStorageMessage[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueueStorageApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Queue Storage middleware pipeline to run each message through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each invocation.</param>
    /// <param name="options">
    /// Configures how a handler's exceptions and failure results are handled, and the batch fan-out
    /// concurrency. Defaults to a new <see cref="QueueStorageOptions"/> instance (safe-by-default:
    /// <see cref="QueueStorageOptions.RaiseOnFailureStatus"/> on,
    /// <see cref="QueueStorageOptions.CatchExceptions"/> off) if omitted.
    /// </param>
    public QueueStorageApplication(IMiddlewarePipeline<QueueStorageContext> pipeline, IServiceResolverFactory serviceResolverFactory, QueueStorageOptions options = null)
        : base(new QueueStorageBatchApplication(pipeline, options), serviceResolverFactory)
    { }
}

/// <summary>
/// Runs every message in a Queue Storage delivery through the middleware pipeline concurrently, each
/// in its own service scope, applying <see cref="QueueStorageOptions"/> to decide whether a message's
/// exception or failure result is contained (logged) or left to cascade and fail the invocation (so
/// the host's poison handling engages).
/// </summary>
public class QueueStorageBatchApplication : IMiddlewareApplication<QueueStorageMessage[]>
{
    private readonly IMiddlewarePipeline<QueueStorageContext> _pipeline;
    private readonly QueueStorageOptions _options;

    public QueueStorageBatchApplication(IMiddlewarePipeline<QueueStorageContext> pipeline, QueueStorageOptions options = null)
    {
        _pipeline = new TransportMiddlewarePipeline<QueueStorageContext>(TransportNames.QueueStorage, pipeline);
        _options = options ?? new QueueStorageOptions();
    }

    public async Task HandleAsync(QueueStorageMessage[] @event, IServiceResolverFactory serviceResolverFactory)
    {
        var contexts = @event.Select(message => new QueueStorageContext(message));
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
                        throw new QueueStorageMessageProcessingException(context.Message.MessageId ?? "unknown");
                    }
                }
                catch (Exception ex) when (_options.CatchExceptions)
                {
                    using (var loggingScope = serviceResolverFactory.CreateScope())
                    {
                        loggingScope.GetService<ILogger<QueueStorageApplication>>()
                            .LogError(ex, "Processing Queue Storage message {messageId} failed", context.Message.MessageId);
                    }
                }
            }, _options.MaxDegreeOfParallelism);
    }
}
