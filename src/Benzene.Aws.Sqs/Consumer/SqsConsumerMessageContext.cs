using Amazon.SQS.Model;
using Benzene.Abstractions.Results;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerMessageContext : IHasMessageResult
{
    private SqsConsumerMessageContext(Message message)
    {
        Message = message;
        MessageResult = Benzene.Core.MessageHandlers.MessageResult.Empty();
    }

    public static SqsConsumerMessageContext CreateInstance(Message message)
    {
        return new SqsConsumerMessageContext(message);
    }

    public Message Message { get; }
    public IMessageResult MessageResult { get; set; }
}
