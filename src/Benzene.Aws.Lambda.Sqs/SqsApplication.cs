using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;

namespace Benzene.Aws.Lambda.Sqs;

/// <summary>
/// Processes an SQS batch event by running each record through the middleware pipeline concurrently,
/// reporting per-record failures back to SQS for partial batch retry.
/// </summary>
public class SqsApplication : IMiddlewareApplication<SQSEvent, SQSBatchResponse>
{
    private readonly IMiddlewarePipeline<SqsMessageContext> _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built SQS middleware pipeline to run each record through.</param>
    public SqsApplication(IMiddlewarePipeline<SqsMessageContext> pipeline)
    {
        _pipeline = pipeline;
    }

    /// <summary>
    /// Handles an SQS batch event, running each record through the pipeline in its own service scope.
    /// </summary>
    /// <param name="event">The SQS batch event to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to create a scope per record.</param>
    /// <returns>
    /// A task that resolves to a batch response listing the records that failed, so SQS can retry only
    /// those. A record fails either if its handler reported an unsuccessful result, or if processing it
    /// threw an exception.
    /// </returns>
    public async Task<SQSBatchResponse> HandleAsync(SQSEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var batchItemFailures = new List<SQSBatchResponse.BatchItemFailure>();
        var tasks = @event.Records.Select(record => SqsMessageContext.CreateInstance(@event, record)).Select(async context =>
            {
                try
                {
                    using (var scope = serviceResolverFactory.CreateScope())
                    {
                        var setCurrentTransport = scope.GetService<ISetCurrentTransport>();
                        setCurrentTransport.SetTransport("sqs");
                        await _pipeline.HandleAsync(context, scope);
                    }

                    if (context.IsSuccessful.HasValue && !context.IsSuccessful.Value)
                    {
                        batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = context.SqsMessage.MessageId });
                    }
                }
                catch (Exception ex)
                {
                    batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = context.SqsMessage.MessageId });
                }
            })
            .ToArray();

        await Task.WhenAll(tasks);
        return new SQSBatchResponse(batchItemFailures);
    }
}
