using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.S3Events;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Microsoft.Extensions.Logging;

namespace Benzene.Aws.Lambda.S3;

/// <summary>
/// Processes an S3 event notification batch by mapping each record to an <see cref="S3RecordContext"/>
/// and running them all through the middleware pipeline concurrently, tagging the transport as
/// <c>"s3"</c> for the duration. Exception/failure-status behavior is configurable via
/// <see cref="S3Options"/>, mirroring <c>SnsApplication</c>.
/// </summary>
public class S3Application : IMiddlewareApplication<S3Event>
{
    private readonly IMiddlewarePipeline<S3RecordContext> _pipeline;
    private readonly S3Options _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="S3Application"/> class.
    /// </summary>
    /// <param name="pipeline">The built S3 middleware pipeline to run each record through.</param>
    /// <param name="options">
    /// Configures how a handler's exceptions and failure results are handled, and the batch fan-out
    /// concurrency. Defaults to a new <see cref="S3Options"/> instance (safe-by-default:
    /// <see cref="S3Options.RaiseOnFailureStatus"/> on, <see cref="S3Options.CatchExceptions"/> off,
    /// unbounded fan-out) if omitted.
    /// </param>
    public S3Application(IMiddlewarePipeline<S3RecordContext> pipeline, S3Options options = null)
    {
        _pipeline = new TransportMiddlewarePipeline<S3RecordContext>(TransportNames.S3, pipeline);
        _options = options ?? new S3Options();
    }

    /// <summary>
    /// Handles an S3 event notification batch, running each record through the pipeline in its own
    /// service scope. Whether a failure result propagates out (failing the invocation, so S3's
    /// async-invoke retry applies) is governed by <see cref="S3Options.RaiseOnFailureStatus"/>/
    /// <see cref="S3Options.CatchExceptions"/>.
    /// </summary>
    /// <param name="event">The S3 event notification batch to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to create a scope per record.</param>
    public async Task HandleAsync(S3Event @event, IServiceResolverFactory serviceResolverFactory)
    {
        var contexts = @event.Records.Select(record => S3RecordContext.CreateInstance(@event, record));
        await BoundedFanOut.WhenAllAsync(contexts, async context =>
            {
                var objectKey = context.S3EventNotificationRecord?.S3?.Object?.Key;
                try
                {
                    using (var scope = serviceResolverFactory.CreateScope())
                    {
                        await _pipeline.HandleAsync(context, scope);
                    }

                    if (_options.RaiseOnFailureStatus && context.MessageResult?.IsSuccessful == false)
                    {
                        throw new S3MessageProcessingException(objectKey);
                    }
                }
                catch (Exception ex) when (_options.CatchExceptions)
                {
                    using var loggingScope = serviceResolverFactory.CreateScope();
                    loggingScope.GetService<ILogger<S3Application>>()
                        .LogError(ex, "Processing S3 object {objectKey} failed", objectKey);
                }
            }, _options.MaxDegreeOfParallelism);
    }
}
