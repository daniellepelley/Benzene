using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.QueueStorage;

/// <summary>
/// The entry point application for a Queue Storage-triggered Azure Function. Maps each message to a
/// <see cref="QueueStorageContext"/> and runs it through the middleware pipeline, tagging the
/// transport as <c>"queue-storage"</c> for the duration. The Queue Storage trigger delivers one
/// message per invocation; the array event shape exists so tests (and any future batched dispatch)
/// can hand several through one call.
/// </summary>
public class QueueStorageApplication : EntryPointMiddlewareApplication<QueueStorageMessage[]>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueueStorageApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built Queue Storage middleware pipeline to run each message through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each invocation.</param>
    /// <param name="maxDegreeOfParallelism">
    /// Optionally caps how many messages from a batched delivery run at once; <c>null</c> (the
    /// default) leaves the fan-out unbounded - the original behavior. Has no effect on the default
    /// one-message-per-invocation trigger cardinality.
    /// </param>
    public QueueStorageApplication(IMiddlewarePipeline<QueueStorageContext> pipeline, IServiceResolverFactory serviceResolverFactory, int? maxDegreeOfParallelism = null)
        : base(new MiddlewareMultiApplication<QueueStorageMessage[], QueueStorageContext>(
                new TransportMiddlewarePipeline<QueueStorageContext>(TransportNames.QueueStorage, pipeline),
                messages => messages.Select(message => new QueueStorageContext(message)).ToArray(),
                maxDegreeOfParallelism),
            serviceResolverFactory)
    { }
}
