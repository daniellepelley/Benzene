using System;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Info;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages;
using Benzene.Core.Middleware;
using Benzene.Core.Request;
using Benzene.Core.Response;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Http;

namespace Benzene.Aws.ApiGateway;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddApiGateway(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.TryAddScoped<IMessageTopicGetter<ApiGatewayContext>, ApiGatewayMessageTopicGetter>();
        services.TryAddScoped<IMessageHeadersGetter<ApiGatewayContext>, ApiGatewayMessageHeadersGetter>();
        services.TryAddScoped<IMessageBodyGetter<ApiGatewayContext>, ApiGatewayMessageBodyGetter>();
        services.TryAddScoped<IMessageHandlerResultSetter<ApiGatewayContext>, ApiGatewayMessageMessageHandlerResultSetter>();
        services
            .AddScoped<IRequestMapper<ApiGatewayContext>,
                MultiSerializerOptionsRequestMapper<ApiGatewayContext, JsonSerializer>>();
        services.AddScoped<IRequestEnricher<ApiGatewayContext>, ApiGatewayRequestEnricher>();
        services.AddScoped<IHttpRequestAdapter<ApiGatewayContext>, ApiGatewayHttpRequestAdapter>();
        services.AddScoped<IBenzeneResponseAdapter<ApiGatewayContext>, ApiGatewayResponseAdapter>();
        services.TryAddScoped<IHttpHeaderMappings, DefaultHttpHeaderMappings>();
        services.AddScoped<IResponseHandler<ApiGatewayContext>, HttpStatusCodeResponseHandler<ApiGatewayContext>>();
        services
            .AddScoped<IResponseHandler<ApiGatewayContext>,
                ResponseHandler<JsonSerializationResponseHandler<ApiGatewayContext>, ApiGatewayContext>>();
        
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("api-gateway"));
        services.AddHttpMessageHandlers();

        return services;
    }

    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseHealthCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string method, string path,
        params IHealthCheck[] healthChecks)
    {
        return app.UseHealthCheck(Constants.DefaultHealthCheckTopic, method, path, healthChecks);
    }

    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseHealthCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string topic, string method, string path,
        params IHealthCheck[] healthChecks)
    {
        return app.UseHealthCheck(topic, method, path, x => x.AddHealthChecks(healthChecks));
    }
    
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseHealthCheck(this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string method, string path, Action<IHealthCheckBuilder> action)
    {
        return app.UseHealthCheck(Constants.DefaultHealthCheckTopic, method, path, action);
    }

    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseHealthCheck(this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string topic, string method, string path, Action<IHealthCheckBuilder> action)
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);
        return app.UseHealthCheck(topic, method, path, builder);
    }

    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseHealthCheck(this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string topic, string method, string path, IHealthCheckBuilder builder)
    {
        return app.Use(resolver => new FuncWrapperMiddleware<ApiGatewayContext>("HealthCheck", async (context, next) =>
        {
            var resultSetter = resolver.GetService<IMessageHandlerResultSetter<ApiGatewayContext>>();
            if (context.ApiGatewayProxyRequest.HttpMethod.ToUpperInvariant() == method.ToUpperInvariant() &&
                context.ApiGatewayProxyRequest.Path == path)
            {
                var result = await HealthCheckProcessor.PerformHealthChecksAsync(topic, builder.GetHealthChecks(resolver));
                resultSetter.SetResultAsync(context, new MessageHandlerResult(new Topic(topic), MessageHandlerDefinition.Empty(), result));
            }
            else
            {
                await next();
            }
        }));
    }
}


