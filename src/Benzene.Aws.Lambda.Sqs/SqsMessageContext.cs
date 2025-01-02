using Amazon.Lambda.SQSEvents;

namespace Benzene.Aws.Lambda.Sqs;

public class SqsMessageContext 
{
    private SqsMessageContext(SQSEvent sqsEvent, SQSEvent.SQSMessage sqsMessage)
    {
        SqsMessage = sqsMessage;
        SqsEvent = sqsEvent;
        // MessageResult = Benzene.Core.MessageHandlers.MessageResult.Empty();
    }

    public static SqsMessageContext CreateInstance(SQSEvent sqsEvent, SQSEvent.SQSMessage sqsMessage)
    {
        return new SqsMessageContext(sqsEvent, sqsMessage);
    }

    public SQSEvent SqsEvent { get; }
    public SQSEvent.SQSMessage SqsMessage { get; }
    // public IMessageResult MessageResult { get; set; }
    public bool? IsSuccessful { get; set; }
}
