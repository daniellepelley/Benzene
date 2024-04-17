using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.Middleware;

namespace Benzene.Core.MiddlewareBuilder;

public static class MiddlewarePipelineBuilderExtensions
{
    public static IMiddlewarePipeline<TContext> AsPipeline<TContext>(this IMiddlewarePipelineBuilder<TContext> source)
    {
        return new MiddlewarePipeline<TContext>(source.GetItems());
    } 
}