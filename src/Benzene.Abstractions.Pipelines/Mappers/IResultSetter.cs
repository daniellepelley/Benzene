using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.Mappers;

public interface IResultSetter<TContext>
{
    Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}