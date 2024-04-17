﻿using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Request;
using Benzene.Core.Helper;

namespace Benzene.Core.Request;

public class EnrichingRequestMapper<TContext> : IRequestMapper<TContext>
{
    private readonly IEnumerable<IRequestEnricher<TContext>> _enrichers;
    private readonly IRequestMapper<TContext> _requestMapper;

    public EnrichingRequestMapper(IRequestMapper<TContext> requestMapper, IEnumerable<IRequestEnricher<TContext>> enrichers)
    {
        _enrichers = enrichers;
        _requestMapper = requestMapper;
    }

    public TRequest GetBody<TRequest>(TContext context) where TRequest : class
    {
        var request = _requestMapper.GetBody<TRequest>(context);
        
        var dictionary = _enrichers.Aggregate(new Dictionary<string, object>() as IDictionary<string, object>, (current, enricher) =>
            DictionaryUtils.MapOnto(current, enricher.Enrich(request, context)));

        return DictionaryUtils.Enrich(request, dictionary);
    }
}