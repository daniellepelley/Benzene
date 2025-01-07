using Amazon.Lambda.Model;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Middleware.BenzeneClient;
using Benzene.Abstractions.Serialization;
using Benzene.Clients.Common;
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

    public LambdaSendMessageContext CreateRequest(IBenzeneClientContext<T, Void> contextIn)
    {
        return new LambdaSendMessageContext(new InvokeRequest
        {
            Payload = _serializer.Serialize(contextIn.Request.Message)
        });
    }

    public void MapResponse(IBenzeneClientContext<T, Void> contextIn, LambdaSendMessageContext contextOut)
    {
        contextIn.Response = BenzeneResult.Accepted<Void>();
    }
}