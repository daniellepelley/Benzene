using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Clients.Aws.Sns;

public class SnsContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, SnsSendMessageContext>
{
    private readonly ISerializer _serializer;
    private readonly string _topicArn;

    public SnsContextConverter(string topicArn)
        :this(new JsonSerializer())
    {
        _topicArn = topicArn;
    }
    
    public SnsContextConverter(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public Task<SnsSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        return Task.FromResult(new SnsSendMessageContext(new PublishRequest
        {
            TopicArn = _topicArn,
            Message = _serializer.Serialize(contextIn.Request.Message)
        }));
    }

    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, SnsSendMessageContext contextOut)
    {
        contextIn.Response = contextOut.Response.HttpStatusCode.Convert<Void>();
        return Task.CompletedTask;
    }
}