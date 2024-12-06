using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.Middleware;

public interface IMiddlewarePipeline<TContext>
{
    Task HandleAsync(TContext context, IServiceResolver serviceResolver);
}