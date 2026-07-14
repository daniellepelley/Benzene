using System;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Http;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Provides extension methods for registering API Gateway services and adding API Gateway-specific
/// health check endpoints.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to process API Gateway requests: request mapping, response
    /// handling, routing, and transport info.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="Extensions.UseApiGateway"/>; you don't normally need to call
    /// this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddApiGateway(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.TryAddScoped<IMessageTopicGetter<ApiGatewayContext>, ApiGatewayMessageTopicGetter>();
        services.TryAddScoped<IMessageHeadersGetter<ApiGatewayContext>, ApiGatewayMessageHeadersGetter>();
        services.TryAddScoped<IMessageBodyGetter<ApiGatewayContext>, ApiGatewayMessageBodyGetter>();
        services.TryAddScoped<IMessageHandlerResultSetter<ApiGatewayContext>, ApiGatewayMessageMessageHandlerResultSetter>();
        services
            .AddScoped<IRequestMapper<ApiGatewayContext>,
                MultiSerializerOptionsRequestMapper<ApiGatewayContext>>();
        services.AddScoped<IRequestEnricher<ApiGatewayContext>, ApiGatewayRequestEnricher>();
        services.AddScoped<IHttpRequestAdapter<ApiGatewayContext>, ApiGatewayHttpRequestAdapter>();
        services.AddScoped<IBenzeneResponseAdapter<ApiGatewayContext>, ApiGatewayResponseAdapter>();
        services.TryAddScoped<IHttpHeaderMappings, DefaultHttpHeaderMappings>();
        services.AddScoped<IResponseHandler<ApiGatewayContext>, HttpStatusCodeResponseHandler<ApiGatewayContext>>();
        services.AddScoped<IResponseHandler<ApiGatewayContext>, SerializationResponseHandler<ApiGatewayContext>>();
        services.AddMediaFormatNegotiation<ApiGatewayContext>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("api-gateway"));
        services.AddHttpMessageHandlers();

        return services;
    }

    /// <summary>
    /// Adds a health check endpoint at the given method and path, using the default health check topic.
    /// </summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="method">The HTTP method the health check responds to.</param>
    /// <param name="path">The HTTP path the health check responds to.</param>
    /// <param name="healthChecks">The health checks to run.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseHealthCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string method, string path,
        params IHealthCheck[] healthChecks)
    {
        return app.UseHealthCheck(Constants.DefaultHealthCheckTopic, method, path, healthChecks);
    }

    /// <summary>
    /// Adds a health check endpoint at the given topic, method, and path.
    /// </summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="topic">The message topic to associate with the health check result.</param>
    /// <param name="method">The HTTP method the health check responds to.</param>
    /// <param name="path">The HTTP path the health check responds to.</param>
    /// <param name="healthChecks">The health checks to run.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseHealthCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string topic, string method, string path,
        params IHealthCheck[] healthChecks)
    {
        return app.UseHealthCheck(topic, method, path, x => x.AddHealthChecks(healthChecks));
    }

    /// <summary>
    /// Adds a health check endpoint at the given method and path, using the default health check topic
    /// and a health check builder action.
    /// </summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="method">The HTTP method the health check responds to.</param>
    /// <param name="path">The HTTP path the health check responds to.</param>
    /// <param name="action">The action that configures the health check builder.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseHealthCheck(this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string method, string path, Action<IHealthCheckBuilder> action)
    {
        return app.UseHealthCheck(Constants.DefaultHealthCheckTopic, method, path, action);
    }

    /// <summary>
    /// Adds a health check endpoint at the given topic, method, and path, using a health check builder action.
    /// </summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="topic">The message topic to associate with the health check result.</param>
    /// <param name="method">The HTTP method the health check responds to.</param>
    /// <param name="path">The HTTP path the health check responds to.</param>
    /// <param name="action">The action that configures the health check builder.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseHealthCheck(this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string topic, string method, string path, Action<IHealthCheckBuilder> action)
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);
        return app.UseHealthCheck(topic, method, path, builder);
    }

    /// <summary>
    /// Adds a health check endpoint at the given topic, method, and path, using a pre-configured health check builder.
    /// </summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="topic">The message topic to associate with the health check result.</param>
    /// <param name="method">The HTTP method the health check responds to.</param>
    /// <param name="path">The HTTP path the health check responds to.</param>
    /// <param name="builder">The pre-configured health check builder.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    /// <remarks>
    /// If the incoming request's method and path match, health checks are run and the result is set on
    /// the context; otherwise, the request is passed to the next middleware.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseHealthCheck(this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string topic, string method, string path, IHealthCheckBuilder builder)
    {
        return app.Use(resolver => new FuncWrapperMiddleware<ApiGatewayContext>("HealthCheck", async (context, next) =>
        {
            var resultSetter = resolver.GetService<IMessageHandlerResultSetter<ApiGatewayContext>>();
            if (context.ApiGatewayProxyRequest.HttpMethod.ToUpperInvariant() == method.ToUpperInvariant() &&
                context.ApiGatewayProxyRequest.Path == path)
            {
                var result = await HealthCheckProcessor.PerformHealthChecksAsync(topic, builder.GetHealthChecks(resolver));
                await resultSetter.SetResultAsync(context, new MessageHandlerResult(new Topic(topic), MessageHandlerDefinition.Empty(), result));
            }
            else
            {
                await next();
            }
        }));
    }

    /// <summary>
    /// Adds a Kubernetes-style liveness-check endpoint at <c>GET /livez</c>, using
    /// <see cref="HealthChecks.Constants.DefaultLivenessTopic"/>. See
    /// <see cref="Benzene.HealthChecks.Extensions.UseLivenessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>
    /// for the liveness/readiness distinction.
    /// </summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="healthChecks">The health checks to run.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseLivenessCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, params IHealthCheck[] healthChecks)
    {
        return app.UseLivenessCheck("/livez", healthChecks);
    }

    /// <summary>Same as <see cref="UseLivenessCheck(IMiddlewarePipelineBuilder{ApiGatewayContext}, IHealthCheck[])"/>, at a custom <paramref name="path"/> instead of the conventional <c>/livez</c>.</summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="path">The HTTP path to respond on.</param>
    /// <param name="healthChecks">The health checks to run.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseLivenessCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string path, params IHealthCheck[] healthChecks)
    {
        return app.UseLivenessCheck(path, x => x.AddHealthChecks(healthChecks));
    }

    /// <summary>Same as <see cref="UseLivenessCheck(IMiddlewarePipelineBuilder{ApiGatewayContext}, IHealthCheck[])"/>, configuring the checks to run via <paramref name="action"/>.</summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="action">Configures the health checks to register.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseLivenessCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, Action<IHealthCheckBuilder> action)
    {
        return app.UseLivenessCheck("/livez", action);
    }

    /// <summary>Same as <see cref="UseLivenessCheck(IMiddlewarePipelineBuilder{ApiGatewayContext}, Action{IHealthCheckBuilder})"/>, at a custom <paramref name="path"/> instead of the conventional <c>/livez</c>.</summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="path">The HTTP path to respond on.</param>
    /// <param name="action">Configures the health checks to register.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseLivenessCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string path, Action<IHealthCheckBuilder> action)
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);
        return app.UseHealthCheck(HealthChecks.Constants.DefaultLivenessTopic, "GET", path, builder);
    }

    /// <summary>
    /// Adds a Kubernetes-style readiness-check endpoint at <c>GET /readyz</c>, using
    /// <see cref="HealthChecks.Constants.DefaultReadinessTopic"/>. See
    /// <see cref="Benzene.HealthChecks.Extensions.UseLivenessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>
    /// for the liveness/readiness distinction.
    /// </summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="healthChecks">The health checks to run.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseReadinessCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, params IHealthCheck[] healthChecks)
    {
        return app.UseReadinessCheck("/readyz", healthChecks);
    }

    /// <summary>Same as <see cref="UseReadinessCheck(IMiddlewarePipelineBuilder{ApiGatewayContext}, IHealthCheck[])"/>, at a custom <paramref name="path"/> instead of the conventional <c>/readyz</c>.</summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="path">The HTTP path to respond on.</param>
    /// <param name="healthChecks">The health checks to run.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseReadinessCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string path, params IHealthCheck[] healthChecks)
    {
        return app.UseReadinessCheck(path, x => x.AddHealthChecks(healthChecks));
    }

    /// <summary>Same as <see cref="UseReadinessCheck(IMiddlewarePipelineBuilder{ApiGatewayContext}, IHealthCheck[])"/>, configuring the checks to run via <paramref name="action"/>.</summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="action">Configures the health checks to register.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseReadinessCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, Action<IHealthCheckBuilder> action)
    {
        return app.UseReadinessCheck("/readyz", action);
    }

    /// <summary>Same as <see cref="UseReadinessCheck(IMiddlewarePipelineBuilder{ApiGatewayContext}, Action{IHealthCheckBuilder})"/>, at a custom <paramref name="path"/> instead of the conventional <c>/readyz</c>.</summary>
    /// <param name="app">The pipeline builder to add the health check to.</param>
    /// <param name="path">The HTTP path to respond on.</param>
    /// <param name="action">Configures the health checks to register.</param>
    /// <returns>The pipeline builder for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<ApiGatewayContext> UseReadinessCheck(
        this IMiddlewarePipelineBuilder<ApiGatewayContext> app, string path, Action<IHealthCheckBuilder> action)
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);
        return app.UseHealthCheck(HealthChecks.Constants.DefaultReadinessTopic, "GET", path, builder);
    }
}

