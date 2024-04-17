using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Request;

public class MultiSerializerOptionsRequestMapper<TContext, TDefaultSerializer> : IRequestMapper<TContext>
    where TDefaultSerializer : class, ISerializer
{
    private readonly IEnumerable<ISerializerOption<TContext>> _options;
    private readonly IServiceResolver _serviceResolver;
    private readonly IEnumerable<IRequestEnricher<TContext>> _enrichers;
    private readonly IMessageBodyMapper<TContext> _messageBodyMapper;

    public MultiSerializerOptionsRequestMapper(
        IServiceResolver serviceResolver,
        IMessageBodyMapper<TContext> messageBodyMapper,
        IEnumerable<ISerializerOption<TContext>> options,
        IEnumerable<IRequestEnricher<TContext>> enrichers)
    {
        _messageBodyMapper = messageBodyMapper;
        _enrichers = enrichers;
        _options = options;
        _serviceResolver = serviceResolver;
    }

    public TRequest GetBody<TRequest>(TContext context) where TRequest : class
    {
        var mapper = GetMapper(context);
        return mapper?.GetBody<TRequest>(context);
    }

    private IRequestMapper<TContext> GetMapper(TContext context)
    {
        var serializerOption = _options.FirstOrDefault(option => option.CanHandle(context));
        var serializer = serializerOption != null
            ? serializerOption.GetSerializer(_serviceResolver)
            : _serviceResolver.GetService<TDefaultSerializer>();
        
        return new EnrichingRequestMapper<TContext>(
                        new RequestMapper<TContext>(_messageBodyMapper, serializer),
                        _enrichers);
    }
}