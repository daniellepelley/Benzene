using System;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.Request;


public class RequestMapBuilder<TContext> : IRequestMapBuilder<TContext>
{
    private readonly Action<Action<IBenzeneServiceContainer>> _register;

    public RequestMapBuilder(Action<Action<IBenzeneServiceContainer>> register)
    {
        _register = register;
    }

    public void Register(Action<IBenzeneServiceContainer> action)
    {
        _register(action);
    }

    public IRequestMapBuilder<TContext> Use<T>() where T : class, ISerializer
    {
        _register(x => x.AddScoped<ISerializerOption<TContext>>(_ => new SerializerOption<TContext, T>(o => o.Always())));
        return this;
    }

    public IRequestMapBuilder<TContext> Use(ISerializer serializer)
    {
        _register(x => x.AddScoped(_ => new InlineSerializerOption<TContext>(_ => true, serializer)));
        return this;
    }

    public IRequestMapBuilder<TContext> UseDefault<T>() where T : class, ISerializer
    {
        _register(x => x.AddScoped(_ => new SerializerOption<TContext, T>(x => x.Always())));
        return this; 
    }
}
