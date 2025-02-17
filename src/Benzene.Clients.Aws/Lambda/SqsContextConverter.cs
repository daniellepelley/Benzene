using System.Threading.Tasks;
using Amazon.Lambda.Model;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Results;

namespace Benzene.Clients.Aws.Lambda;

public class LambdaContextConverter<T> : IContextConverter<IBenzeneClientContext<T, Void>, LambdaSendMessageContext>
{
    private readonly ISerializer _serializer;

    public LambdaContextConverter()
        :this(new JsonSerializer())
    { }
    
    public LambdaContextConverter(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public Task<LambdaSendMessageContext> CreateRequestAsync(IBenzeneClientContext<T, Void> contextIn)
    {
        return Task.FromResult(new LambdaSendMessageContext(new InvokeRequest
        {
            Payload = _serializer.Serialize(contextIn.Request.Message)
        }));
    }

    public Task MapResponseAsync(IBenzeneClientContext<T, Void> contextIn, LambdaSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
        return Task.CompletedTask;
    }
}