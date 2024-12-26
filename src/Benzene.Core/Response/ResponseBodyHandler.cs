using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Response;

public class ResponseBodyHandler<TContext> : ISyncResponseHandler<TContext>
{
    private readonly IResponsePayloadMapper<TContext> _responsePayloadMapper;
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;
    private readonly ISerializer _serializer;

    public ResponseBodyHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter, IResponsePayloadMapper<TContext> responsePayloadMapper, ISerializer serializer)
    {
        _serializer = serializer;
        _benzeneResponseAdapter = benzeneResponseAdapter;
        _responsePayloadMapper = responsePayloadMapper;
    }

    public void HandleAsync(TContext context, IMessageHandlerResult result)
    {
        _benzeneResponseAdapter.SetBody(context, _responsePayloadMapper.Map(context, result, _serializer));
        _benzeneResponseAdapter.SetContentType(context, Constants.JsonContentType);
    }
}
