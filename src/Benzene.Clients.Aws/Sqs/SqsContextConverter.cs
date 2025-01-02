using Amazon.SQS.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Clients.Aws.Common;
using Benzene.Results;

namespace Benzene.Clients.Aws.Sqs;

public class SqsContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, SqsSendMessageContext>
{
    private readonly ISerializer _serializer;
    private string _queueUrl;

    public SqsContextConverter(string queueUrl)
        :this(queueUrl, new JsonSerializer())
    { }
    
    public SqsContextConverter(string queueUrl, ISerializer serializer)
    {
        _queueUrl = queueUrl;
        _serializer = serializer;
    }

    public SqsSendMessageContext CreateRequest(IBenzeneClientContext<T, Void> contextIn)
    {
        return new SqsSendMessageContext(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = _serializer.Serialize(contextIn.Request.Message)
        });
    }

    public void MapResponse(IBenzeneClientContext<T, Void> contextIn, SqsSendMessageContext contextOut)
    {
        contextIn.Response = contextOut.Response.HttpStatusCode.Convert<Void>();
    }
}