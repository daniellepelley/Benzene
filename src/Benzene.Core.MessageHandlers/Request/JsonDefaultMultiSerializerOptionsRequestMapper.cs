using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.Serialization;

namespace Benzene.Core.Request;

public class JsonDefaultMultiSerializerOptionsRequestMapper<TContext> : MultiSerializerOptionsRequestMapper<TContext, JsonSerializer> 
{
    public JsonDefaultMultiSerializerOptionsRequestMapper(
        IServiceResolver serviceResolver,
        IMessageBodyMapper<TContext> messageBodyMapper,
        IEnumerable<ISerializerOption<TContext>> options,
        IEnumerable<IRequestEnricher<TContext>> enrichers)
        : base(serviceResolver, messageBodyMapper, options, enrichers)
    { } 
}
