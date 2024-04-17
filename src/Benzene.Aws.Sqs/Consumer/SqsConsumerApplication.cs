using System.Linq;
using Amazon.SQS.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerApplication : MiddlewareMultiApplication<ReceiveMessageResponse, SqsConsumerMessageContext>
{
    public SqsConsumerApplication(IMiddlewarePipeline<SqsConsumerMessageContext> pipeline)
        :base("sqs", pipeline, @event => @event.Messages.Select(SqsConsumerMessageContext.CreateInstance).ToArray())
    { }
}
