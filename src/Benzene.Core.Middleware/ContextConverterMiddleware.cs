using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides middleware that converts from one context type to another, executes a pipeline with the converted context,
/// and maps the response back to the original context.
/// </summary>
/// <typeparam name="TContext">The input context type to convert from.</typeparam>
/// <typeparam name="TContextOut">The output context type to convert to.</typeparam>
/// <remarks>
/// This middleware enables context transformation within a middleware pipeline, allowing different stages
/// of processing to operate on different context types. The converter handles both the transformation
/// to the new context and the mapping of results back to the original context.
/// </remarks>
public class ContextConverterMiddleware<TContext, TContextOut>(
    IContextConverter<TContext, TContextOut> converter,
    IMiddlewarePipeline<TContextOut> middlewarePipeline,
    IServiceResolver serviceResolver)
    : IMiddleware<TContext>
{
    /// <summary>
    /// Gets the name of this middleware component.
    /// </summary>
    public string Name => "Convert";

    /// <summary>
    /// Handles the middleware execution by converting the context, executing the pipeline with the converted context,
    /// and mapping the response back.
    /// </summary>
    /// <param name="context">The input context to convert.</param>
    /// <param name="next">The next middleware in the pipeline (not invoked by this middleware).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var contextOut = await converter.CreateRequestAsync(context);
        await middlewarePipeline.HandleAsync(contextOut, serviceResolver);
        await converter.MapResponseAsync(context, contextOut);
    }
}