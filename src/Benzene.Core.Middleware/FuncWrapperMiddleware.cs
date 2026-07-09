using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class FuncWrapperMiddleware<TContext>(string name, Func<TContext, Func<Task>, Task> func) : IMiddleware<TContext>
{
    public string Name { get; } = !string.IsNullOrEmpty(name) ? name : Constants.Unnamed;

    public FuncWrapperMiddleware(Func<TContext, Func<Task>, Task> func)
        :this(Constants.Unnamed, func)
    { }

    public Task HandleAsync(TContext context, Func<Task> next) => func(context, next);
}
