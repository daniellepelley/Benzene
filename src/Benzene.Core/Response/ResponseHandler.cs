using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;

namespace Benzene.Core.Response;

public class ResponseHandler<T, TContext> : ISyncResponseHandler<TContext> where T : ISerializationResponseHandler<TContext> where TContext : class, IHasMessageResult
{
    private readonly IResponsePayloadMapper<TContext> _responsePayloadMapper;
    private readonly T _httpSerializationResponseHandler;

    public ResponseHandler(T httpSerializationResponseHandler, IResponsePayloadMapper<TContext> responsePayloadMapper)
    {
        _httpSerializationResponseHandler = httpSerializationResponseHandler;
        _responsePayloadMapper = responsePayloadMapper;
    }

    public void HandleAsync(TContext context)
    {
        var apiGatewayHttpBodySerializer = new BodySerializer<TContext>(_responsePayloadMapper, context);
        _httpSerializationResponseHandler.HandleAsync(context, apiGatewayHttpBodySerializer);
    }
}
