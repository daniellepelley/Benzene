using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Azure.Function.BlobStorage;

/// <summary>
/// The entry point application for a blob-triggered Azure Function. Maps the delivered blob to a
/// <see cref="BlobStorageContext"/> and runs it through the middleware pipeline, tagging the
/// transport as <c>"blob-storage"</c> for the duration.
/// </summary>
public class BlobStorageApplication : EntryPointMiddlewareApplication<BlobTriggerEvent>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobStorageApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built blob middleware pipeline to run each blob through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process each invocation.</param>
    public BlobStorageApplication(IMiddlewarePipeline<BlobStorageContext> pipeline, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareApplication<BlobTriggerEvent, BlobStorageContext>(
                new TransportMiddlewarePipeline<BlobStorageContext>(TransportNames.BlobStorage, pipeline),
                blob => new BlobStorageContext(blob)),
            serviceResolverFactory)
    { }
}
