// using Benzene.Abstractions.MiddlewareBuilder;
// using Benzene.Core.MiddlewareBuilder;
// using Benzene.Http.Cors;
// using Benzene.Http.Routing;
//
// namespace Benzene.Aws.ApiGateway.Cors
// {
//     public static class Extensions
//     {
//         public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseCors(
//             this IMiddlewarePipelineBuilder<ApiGatewayContext> app, CorsSettings corsSettings)
//         {
//             app.Register(x => x.AddSingleton(resolver => new ApiGatewayContextCorsMiddleware(corsSettings, resolver.GetService<IHttpEndpointFinder>())));
//             return app.Use<ApiGatewayContext, ApiGatewayContextCorsMiddleware>();
//         }
//     }
// }
