﻿using System.Diagnostics;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Results;
using Void = Benzene.Results.Void;

namespace Benzene.Core.MessageHandlers;

public class MessageHandler<TRequest, TResponse> : IMessageHandler where TRequest : class
{
    private readonly IMessageHandler<TRequest, TResponse> _inner;
    private readonly IBenzeneLogger _logger;

    public MessageHandler(IMessageHandler<TRequest, TResponse> inner, IBenzeneLogger logger)
    {
        _logger = logger;
        _inner = inner;
    }

    public async Task<IServiceResult> HandlerAsync(IRequestFactory requestFactory)
    {
        TRequest? messageObject;
        try
        {
            messageObject = requestFactory.GetRequest<TRequest>();
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Message is not valid: {ex}");
            _logger.LogWarning(ex, "Message is not valid");
            return ServiceResult.BadRequest("Message is not valid", ex.Message);
        }
        
        try
        {
            var result = await _inner.HandleAsync(messageObject);
            if (result == null)
            {
                return ServiceResult.Accepted<Void>();
            }

            return result;
        }
        catch(ArgumentException ex)
        {
            Debug.WriteLine($"Message handler threw argument exception: {ex}");
            _logger.LogError(ex, "Message handler threw argument exception");
            return ServiceResult.ValidationError(ex.Message);
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Message handler threw an exception: {ex}");
            _logger.LogError(ex, "Message handler threw an exception");
            return ServiceResult.ServiceUnavailable("Message handler threw an exception", ex.Message);
        }
    }
}
