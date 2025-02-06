using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.MessageHandlers.Request;

public class MultiSerializerOptionsRequestMapper<TContext, TDefaultSerializer> : IRequestMapper<TContext>
    where TDefaultSerializer : class, ISerializer
{
    private readonly IEnumerable<ISerializerOption<TContext>> _options;
    private readonly IServiceResolver _serviceResolver;
    private readonly IEnumerable<IRequestEnricher<TContext>> _enrichers;
    private readonly IMessageBodyGetter<TContext> _messageBodyGetter;

    public MultiSerializerOptionsRequestMapper(
        IServiceResolver serviceResolver,
        IMessageBodyGetter<TContext> messageBodyGetter,
        IEnumerable<ISerializerOption<TContext>> options,
        IEnumerable<IRequestEnricher<TContext>> enrichers)
    {
        _messageBodyGetter = messageBodyGetter;
        _enrichers = enrichers;
        _options = options;
        _serviceResolver = serviceResolver;
    }

    public TRequest? GetBody<TRequest>(TContext context) where TRequest : class
    {
        var mapper = GetMapper(context);
        return mapper.GetBody<TRequest>(context);
    }

    private IRequestMapper<TContext> GetMapper(TContext context)
    {
        var serializerOption = _options.FirstOrDefault(option => option.CanHandle.Check(context, _serviceResolver));
        var serializer = serializerOption != null
            ? serializerOption.GetSerializer(_serviceResolver)
            : _serviceResolver.GetService<TDefaultSerializer>();
        
        return new EnrichingRequestMapper<TContext>(
                        new RequestMapper<TContext>(_messageBodyGetter, serializer),
                        _enrichers);
    }
}