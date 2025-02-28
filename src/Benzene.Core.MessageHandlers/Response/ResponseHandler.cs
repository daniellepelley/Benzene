﻿using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.MessageHandlers.Response;

public class ResponseHandler<T, TContext> : ISyncResponseHandler<TContext> where T : ISerializationResponseHandler<TContext> where TContext : class
{
    private readonly IResponsePayloadMapper<TContext> _responsePayloadMapper;
    private readonly T _httpSerializationResponseHandler;

    public ResponseHandler(T httpSerializationResponseHandler, IResponsePayloadMapper<TContext> responsePayloadMapper)
    {
        _httpSerializationResponseHandler = httpSerializationResponseHandler;
        _responsePayloadMapper = responsePayloadMapper;
    }

    public void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        var httpHttpBodySerializer = new BodySerializer<TContext>(_responsePayloadMapper, context);
        _httpSerializationResponseHandler.HandleAsync(context, messageHandlerResult, httpHttpBodySerializer);
    }
}
