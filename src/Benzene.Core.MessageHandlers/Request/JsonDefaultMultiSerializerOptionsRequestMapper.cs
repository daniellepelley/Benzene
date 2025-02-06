using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Core.MessageHandlers.Request;

public class JsonDefaultMultiSerializerOptionsRequestMapper<TContext> : MultiSerializerOptionsRequestMapper<TContext, JsonSerializer> 
{
    public JsonDefaultMultiSerializerOptionsRequestMapper(
        IServiceResolver serviceResolver,
        IMessageBodyGetter<TContext> messageBodyGetter,
        IEnumerable<ISerializerOption<TContext>> options,
        IEnumerable<IRequestEnricher<TContext>> enrichers)
        : base(serviceResolver, messageBodyGetter, options, enrichers)
    { } 
}
