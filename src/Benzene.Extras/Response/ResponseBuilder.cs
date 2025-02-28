﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;

namespace Benzene.Extras.Response;

public class ResponseBuilder<TContext> : IResponseBuilder<TContext> where TContext : class
{
    private readonly List<Func<IServiceResolver, IResponseHandler<TContext>>> _builders = new();
    private readonly IRegisterDependency _register;

    public ResponseBuilder(IRegisterDependency register)
    {
        _register = register;
    }

    public IResponseBuilder<TContext> Add<T>() where T : class, IResponseHandler<TContext>
    {
        _register.Register(x => x.AddScoped<T>());
        _builders.Add(x => x.GetService<T>());
        return this;
    }

    public IResponseBuilder<TContext> Add(Func<IServiceResolver, IResponseHandler<TContext>> func)
    {
        _builders.Add(func);
        return this;
    }

    public Func<IServiceResolver, IResponseHandler<TContext>>[] GetBuilders()
    {
        return _builders.ToArray();
    }
}
