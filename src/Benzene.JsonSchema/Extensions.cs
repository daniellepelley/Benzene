using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Core.Middleware;

namespace Benzene.JsonSchema;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<TContext> UseJsonSchema<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app)
        where TContext : class
    {
        app.Register(x => x.AddJsonSchema());
        return app.Use<TContext, JsonSchemaMiddleware<TContext>>();
    }
}