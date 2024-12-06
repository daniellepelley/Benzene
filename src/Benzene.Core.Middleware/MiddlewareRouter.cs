using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public abstract class MiddlewareRouter<TRequest, TContext> : IMiddleware<TContext>
{
    private readonly IServiceResolver _serviceResolver;

    protected MiddlewareRouter(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
    }

    public string Name => "MiddlewareRouter";

    
    public virtual async Task HandleAsync(TContext context, Func<Task> next)
    {
        var request = TryExtractRequest(context);

        if (request == null)
        {
            await next();
        }
        else
        {
            if (CanHandle(request))
            {
                await HandleFunction(request, context, _serviceResolver);
            }
            else
            {
                await next();
            }
        }
    }

    protected abstract bool CanHandle(TRequest request);
    protected abstract Task HandleFunction(TRequest request, TContext context, IServiceResolver serviceResolver);
    protected abstract TRequest TryExtractRequest(TContext context);
}
