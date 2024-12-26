using Benzene.Abstractions.DI;
using Benzene.Abstractions.Serialization;

namespace Benzene.Abstractions.MessageHandlers.Request;

public interface IRequestMapBuilder<TContext>
{
    void Register(Action<IBenzeneServiceContainer> action);
    IRequestMapBuilder<TContext> Use<T>() where T : class, ISerializer;
    IRequestMapBuilder<TContext> Use(ISerializer serializer);
    IRequestMapBuilder<TContext> UseDefault<T>() where T : class, ISerializer;
}