using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class ContextConverterMiddleware<TContext, TContextOut>(
    IContextConverter<TContext, TContextOut> converter,
    IMiddlewarePipeline<TContextOut> middlewarePipeline,
    IServiceResolver serviceResolver)
    : IMiddleware<TContext>
{
    public string Name => "Convert";

    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var contextOut = await converter.CreateRequestAsync(context);
        await middlewarePipeline.HandleAsync(contextOut, serviceResolver);
        await converter.MapResponseAsync(context, contextOut);
    }
}