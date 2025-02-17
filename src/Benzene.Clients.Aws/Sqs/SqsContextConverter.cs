using System.Threading.Tasks;
using Amazon.SQS.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
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

    public Task<SqsSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    { 
        return Task.FromResult(new SqsSendMessageContext(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = _serializer.Serialize(contextIn.Request.Message)
        }));
    }

    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, SqsSendMessageContext contextOut)
    {
        contextIn.Response = contextOut.Response.HttpStatusCode.Convert<Void>();
        return Task.CompletedTask;
    }
}