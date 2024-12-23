using Amazon.SQS.Model;

namespace Benzene.Clients.Aws.Sqs;

public class SqsSendMessageContext
{
    public SqsSendMessageContext(SendMessageRequest request)
    {
        Request = request;
    }
    public SendMessageRequest Request { get; }
    public SendMessageResponse Response { get; set; }
}