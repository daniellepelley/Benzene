using Amazon.SQS.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Clients.Aws.Sqs;

public class SqsContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, SqsSendMessageContext>
{
    private readonly ISerializer _serializer;

    public SqsContextConverter()
        :this(new JsonSerializer())
    { }
    
    public SqsContextConverter(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public SqsSendMessageContext CreateRequest(IBenzeneClientContext<T, Results.Void> contextIn)
    {
        return new SqsSendMessageContext(new SendMessageRequest
        {
            MessageBody = _serializer.Serialize(contextIn.Request.Message)
        });
    }

    public void MapResponse(IBenzeneClientContext<T, Results.Void> contextIn, SqsSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Results.Void>();
    }
}