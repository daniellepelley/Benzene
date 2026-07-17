using Benzene.Abstractions.DI;
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
    public QueueStorageApplication(IMiddlewarePipeline<QueueStorageContext> pipeline, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareMultiApplication<QueueStorageMessage[], QueueStorageContext>(
                new TransportMiddlewarePipeline<QueueStorageContext>("queue-storage", pipeline),
                messages => messages.Select(message => new QueueStorageContext(message)).ToArray()),
            serviceResolverFactory)
    { }
}
