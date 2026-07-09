using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public abstract class MiddlewareRouter<TRequest, TContext>(IServiceResolver serviceResolver) : IMiddleware<TContext>
{
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
                await HandleFunction(request, context, serviceResolver.GetService<IServiceResolverFactory>());
            }
            else
            {
                await next();
            }
        }
    }

    protected abstract bool CanHandle(TRequest request);
    protected abstract Task HandleFunction(TRequest request, TContext context, IServiceResolverFactory serviceResolverFactory);
    protected abstract TRequest TryExtractRequest(TContext context);
}
