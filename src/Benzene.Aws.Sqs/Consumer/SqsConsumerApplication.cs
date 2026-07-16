using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Processes a batch of received SQS messages by mapping each to an <see cref="SqsConsumerMessageContext"/>
/// and running them all through the middleware pipeline concurrently, tagging the transport as
/// <c>"sqs"</c> for the duration.
/// </summary>
public class SqsConsumerApplication : IMiddlewareApplication<ReceiveMessageResponse, SqsConsumerBatchResult>
{
    private readonly IMiddlewarePipeline<SqsConsumerMessageContext> _pipeline;
    private readonly SqsConsumerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsConsumerApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built SQS middleware pipeline to run each message through.</param>
    /// <param name="options">
    /// Configures how the batch is acknowledged. Defaults to a new <see cref="SqsConsumerOptions"/>
    /// instance (<see cref="SqsConsumerAckMode.WholeBatch"/>) if omitted.
    /// </param>
    public SqsConsumerApplication(IMiddlewarePipeline<SqsConsumerMessageContext> pipeline, SqsConsumerOptions options = null)
    {
        _pipeline = new TransportMiddlewarePipeline<SqsConsumerMessageContext>("sqs", pipeline);
        _options = options ?? new SqsConsumerOptions();
    }

    /// <summary>
    /// Handles a poll batch, running each message through the pipeline in its own service scope.
    /// </summary>
    /// <param name="event">The poll batch to process.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to create a scope per message.</param>
    /// <returns>
    /// A task that resolves to the batch's per-message outcome. Under <see cref="SqsConsumerAckMode.WholeBatch"/>
    /// (the default), a message's handler throwing propagates out of this call entirely - the returned
    /// result is only reached when every message ran without throwing. Under
    /// <see cref="SqsConsumerAckMode.PerMessage"/>, a message's exception is contained (logged nowhere
    /// by this class - the message is just reported as failed) and never propagates out of this call.
    /// </returns>
    public async Task<SqsConsumerBatchResult> HandleAsync(ReceiveMessageResponse @event, IServiceResolverFactory serviceResolverFactory)
    {
        var failedMessages = new List<Message>();
        var tasks = @event.Messages.Select(message => (Message: message, Context: SqsConsumerMessageContext.CreateInstance(message))).Select(async pair =>
            {
                try
                {
                    using (var scope = serviceResolverFactory.CreateScope())
                    {
                        await _pipeline.HandleAsync(pair.Context, scope);
                    }

                    if (pair.Context.MessageResult?.IsSuccessful == false)
                    {
                        failedMessages.Add(pair.Message);
                    }
                }
                catch (Exception)
                {
                    failedMessages.Add(pair.Message);

                    if (_options.AckMode == SqsConsumerAckMode.WholeBatch)
                    {
                        throw;
                    }
                }
            })
            .ToArray();

        await Task.WhenAll(tasks);

        var successfulMessages = @event.Messages.Except(failedMessages).ToArray();
        return new SqsConsumerBatchResult(successfulMessages, failedMessages);
    }
}
