using Amazon.Lambda.SQSEvents;

namespace Benzene.Aws.Lambda.Sqs;

public class SqsMessageContext 
{
    private SqsMessageContext(SQSEvent sqsEvent, SQSEvent.SQSMessage sqsMessage)
    {
        SqsMessage = sqsMessage;
        SqsEvent = sqsEvent;
    }

    public static SqsMessageContext CreateInstance(SQSEvent sqsEvent, SQSEvent.SQSMessage sqsMessage)
    {
        return new SqsMessageContext(sqsEvent, sqsMessage);
    }

    public SQSEvent SqsEvent { get; }
    public SQSEvent.SQSMessage SqsMessage { get; }
    public bool? IsSuccessful { get; set; }
}
