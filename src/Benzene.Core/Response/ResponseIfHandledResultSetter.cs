﻿using System.Threading.Tasks;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;

namespace Benzene.Core.Response;

public class ResponseIfHandledResultSetter<TContext> : IResultSetter<TContext> where TContext : class
{
    private readonly IResponseHandlerContainer<TContext> _responseHandlerContainer;

    public ResponseIfHandledResultSetter(IResponseHandlerContainer<TContext> responseHandlerContainer)
    {
        _responseHandlerContainer = responseHandlerContainer;
    }

    public async Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        if (messageHandlerResult.Topic != null && messageHandlerResult.Topic.Id != Constants.Missing)
        {
            await _responseHandlerContainer.HandleAsync(context, messageHandlerResult);
        }
    }
}

