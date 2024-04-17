using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Abstractions.Results;
using Benzene.Core.Mappers;
using Benzene.Core.Middleware;

namespace Benzene.Core.Correlation
{
    public static class Extensions
    {
        public static IBenzeneServiceContainer AddCorrelationId(this IBenzeneServiceContainer services)
        {
            return services.AddScoped<ICorrelationId, CorrelationId>();
        }

        public static IMiddlewarePipelineBuilder<TContext> UseCorrelationId<TContext>(
            this IMiddlewarePipelineBuilder<TContext> app, string header = "correlationId")
            where TContext : IHasMessageResult
        {
            app.Register(x => x.AddCorrelationId());

            app.Add(resolver => new FuncWrapperMiddleware<TContext>("CorrelationId", async (context, next) =>
            {
                var setCorrelationId = resolver.GetService<ICorrelationId>();
                var messageMapper = resolver.GetService<IMessageMapper<TContext>>();
                var correlationId = messageMapper.GetHeader(context, header);
                setCorrelationId.Set(correlationId);

                await next();
            }));
            return app;
        }
    }
}
