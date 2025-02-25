﻿using Amazon.SQS.Model;

namespace Benzene.Aws.Sqs.Consumer;

public class SqsConsumerMessageContext 
{
    private SqsConsumerMessageContext(Message message)
    {
        Message = message;
    }

    public static SqsConsumerMessageContext CreateInstance(Message message)
    {
        return new SqsConsumerMessageContext(message);
    }

    public Message Message { get; }
}
