using Amazon.Lambda.SQSEvents;
using Benzene.Abstractions.Results;

namespace Benzene.Aws.Sqs;

public class SqsMessageContext : IHasMessageResult
{
    private SqsMessageContext(SQSEvent sqsEvent, SQSEvent.SQSMessage sqsMessage)
    {
        SqsMessage = sqsMessage;
        SqsEvent = sqsEvent;
        MessageResult = Benzene.Core.Results.MessageResult.Empty();
    }

    public static SqsMessageContext CreateInstance(SQSEvent sqsEvent, SQSEvent.SQSMessage sqsMessage)
    {
        return new SqsMessageContext(sqsEvent, sqsMessage);
    }

    public SQSEvent SqsEvent { get; }
    public SQSEvent.SQSMessage SqsMessage { get; }
    public IMessageResult MessageResult { get; set; }
}
