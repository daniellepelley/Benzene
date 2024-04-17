using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Serialization;
using Benzene.Clients.Aws.Common;
using Benzene.Results;

namespace Benzene.Clients.Aws.Sqs;

public class SqsBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IBenzeneLogger _logger;
    private readonly string _queueUrl;
    private readonly IAmazonSQS _amazonSqsClient;
    private readonly ISerializer _serializer;

    public SqsBenzeneMessageClient(string queueUrl, IAmazonSQS amazonSqsClient, IBenzeneLogger logger)
    {
        _amazonSqsClient = amazonSqsClient;
        _logger = logger;
        _queueUrl = queueUrl;
        _serializer = new JsonSerializer();
    }

    public async Task<IClientResult<TResponse>> SendMessageAsync<TMessage, TResponse>(string topic, TMessage message, IDictionary<string, string> headers)
    {
        try
        {
            var response = await _amazonSqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = _serializer.Serialize(message),
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    { "topic", new MessageAttributeValue { StringValue = topic, DataType = "String"} }
                }
            });

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return ClientResult.Accepted<TResponse>();
            }

            return ClientResultHttpMapper.Map<TResponse>(response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message {receiverTopic} to {receiver} failed", topic, _queueUrl);
            return ClientResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }
        
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}

