using System.Linq;
using Amazon.SQS.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Sqs.Consumer;

/// <summary>
/// Processes a batch of received SQS messages by mapping each to an <see cref="SqsConsumerMessageContext"/>
/// and running them all through the middleware pipeline, tagging the transport as <c>"sqs"</c> for the duration.
/// </summary>
public class SqsConsumerApplication : MiddlewareMultiApplication<ReceiveMessageResponse, SqsConsumerMessageContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqsConsumerApplication"/> class.
    /// </summary>
    /// <param name="pipeline">The built SQS middleware pipeline to run each message through.</param>
    public SqsConsumerApplication(IMiddlewarePipeline<SqsConsumerMessageContext> pipeline)
        :base(
            new TransportMiddlewarePipeline<SqsConsumerMessageContext>("sqs", pipeline),
            @event => @event.Messages.Select(SqsConsumerMessageContext.CreateInstance).ToArray())
    { }
}
