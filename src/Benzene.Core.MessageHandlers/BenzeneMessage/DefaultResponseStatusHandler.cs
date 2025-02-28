﻿using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

public class DefaultResponseStatusHandler<TContext> : ISyncResponseHandler<TContext> where TContext : class
{
    private readonly IBenzeneResponseAdapter<TContext> _benzeneResponseAdapter;

    public DefaultResponseStatusHandler(IBenzeneResponseAdapter<TContext> benzeneResponseAdapter)
    {
        _benzeneResponseAdapter = benzeneResponseAdapter;
    }
    
    public void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        _benzeneResponseAdapter.SetStatusCode(context, messageHandlerResult.BenzeneResult.Status);
    }
}
