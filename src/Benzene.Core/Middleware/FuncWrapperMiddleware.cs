using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class FuncWrapperMiddleware<TContext> : IMiddleware<TContext>
{
    public string Name { get; }
    private readonly Func<TContext, Func<Task>, Task> _func;
    
    public FuncWrapperMiddleware(Func<TContext, Func<Task>, Task> func)
        :this(Constants.Unnamed, func)
    { }

    public FuncWrapperMiddleware(string name, Func<TContext, Func<Task>, Task> func)
    {
        Name = !string.IsNullOrEmpty(name) ? name : Constants.Unnamed;
        _func = func;
    }

    public Task HandleAsync(TContext context, Func<Task> next) => _func(context, next);
}
