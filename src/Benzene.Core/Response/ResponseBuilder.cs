using System;
using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;

namespace Benzene.Core.Response;

public class ResponseBuilder<TContext> : IResponseBuilder<TContext> where TContext : class, IHasMessageResult
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
