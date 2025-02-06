using System.Collections.Generic;
using Amazon.SQS.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Serialization;

namespace Benzene.Clients.Aws.Sqs;

public class SqsClientRequestMapper : IClientRequestMapper<SendMessageRequest>
{
    private readonly string _queueUrl;
    private readonly ISerializer _serializer;

    public SqsClientRequestMapper(string queueUrl, ISerializer serializer)
    {
        _serializer = serializer;
        _queueUrl = queueUrl;
    }

    public SendMessageRequest CreateRequest<TRequest>(IBenzeneClientRequest<TRequest> request)
    {
        return new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = _serializer.Serialize(request.Message),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "topic", new MessageAttributeValue { StringValue = request.Topic, DataType = "String"} }
            }
        };
    }
}