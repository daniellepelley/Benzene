// using Benzene.Abstractions.MiddlewareBuilder;
// using Benzene.Core.Middleware;
// using Benzene.Core.MiddlewareBuilder;
// using Benzene.HealthChecks;
// using Benzene.HealthChecks.Core;
//
// namespace Benzene.SelfHost.Http;
//
// public static class Extensions
// {
//     public static IMiddlewarePipelineBuilder<SelfHostContext> UseHttp(this IMiddlewarePipelineBuilder<SelfHostContext> app, Action<IMiddlewarePipelineBuilder<HttpContext>> action)
//     {
//         app.Register(x => x.AddHttp());
//         var middlewarePipelineBuilder = app.Create<HttpContext>();
//         action(middlewarePipelineBuilder);
//         var pipeline = middlewarePipelineBuilder.Build();
//         return app.Use(resolver => new HttpLambdaHandler(pipeline, resolver));
//     }
//
//     public static IMiddlewarePipelineBuilder<HttpContext> UseHttpResponse(
//         this IMiddlewarePipelineBuilder<HttpContext> app)
//     {
//         return app.UseProcessResponse(x => x.Add<HttpMessageResponseHandler>());
//     }
//
//     public static IMiddlewarePipelineBuilder<HttpContext> UseHealthCheck(
//     this IMiddlewarePipelineBuilder<HttpContext> app, string method, string path,
//     params IHealthCheck[] healthChecks)
//     {
//         return app.UseHealthCheck(Constants.DefaultHealthCheckTopic, method, path, healthChecks);
//     }
//
//     public static IMiddlewarePipelineBuilder<HttpContext> UseHealthCheck(
//         this IMiddlewarePipelineBuilder<HttpContext> app, string topic, string method, string path,
//         params IHealthCheck[] healthChecks)
//     {
//         return app.UseHealthCheck(topic, method, path, x => x.AddHealthChecks(healthChecks));
//     }
//
//     public static IMiddlewarePipelineBuilder<HttpContext> UseHealthCheck(this IMiddlewarePipelineBuilder<HttpContext> app, string method, string path, Action<IHealthCheckBuilder> action)
//     {
//         return app.UseHealthCheck(Constants.DefaultHealthCheckTopic, method, path, action);
//     }
//
//     public static IMiddlewarePipelineBuilder<HttpContext> UseHealthCheck(this IMiddlewarePipelineBuilder<HttpContext> app, string topic, string method, string path, Action<IHealthCheckBuilder> action)
//     {
//         var builder = app.GetHealthCheckerBuilder();
//         action(builder);
//         return app.UseHealthCheck(topic, method, path, builder);
//     }
//
//     public static IMiddlewarePipelineBuilder<HttpContext> UseHealthCheck(this IMiddlewarePipelineBuilder<HttpContext> app, string topic, string method, string path, IHealthCheckBuilder builder)
//     {
//         return app.Use(resolver => new FuncWrapperMiddleware<HttpContext>("HealthCheck", async (context, next) =>
//         {
//             if (context.Request.Method.ToUpperInvariant() == method.ToUpperInvariant() &&
//                 context.Request.Path == path)
//             {
//                 context.MessageResult = await HealthCheckProcessor.PerformHealthChecksAsync(topic, builder.GetHealthChecks(resolver));
//             }
//             else
//             {
//                 await next();
//             }
//         }));
//     }
//
// }
