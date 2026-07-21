using Benzene.Abstractions.DI;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.Core;
using Benzene.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.Middleware;
using Benzene.Http;
using Benzene.Http.RequestBody;
using Benzene.Http.Routing;

namespace Benzene.Azure.Function.AspNet;

/// <summary>
/// Provides extension methods for registering HTTP message-handling services and adding HTTP trigger
/// handling to an <see cref="IAzureFunctionAppBuilder"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds an HTTP entry point application to the Azure Function app, configuring its inner middleware
    /// pipeline.
    /// </summary>
    /// <param name="app">The Azure Function app builder to add HTTP handling to.</param>
    /// <param name="action">The action that configures the HTTP middleware pipeline.</param>
    /// <returns>The Azure Function app builder, for method chaining.</returns>
    public static IAzureFunctionAppBuilder UseHttp(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<AspNetContext>> action)
    {
        app.Register(x => x.AddAspNet());
        var pipeline = app.Create<AspNetContext>();
        // Seed the scope's ambient cancellation token from the request's aborted token, so any
        // component resolving ICancellationTokenAccessor observes a client disconnect / request abort.
        pipeline.Use(resolver => new FuncWrapperMiddleware<AspNetContext>("SeedCancellationToken", async (context, next) =>
        {
            resolver.SeedCancellationToken(context.HttpRequest.HttpContext.RequestAborted);
            await next();
        }));
        // Read the request body asynchronously, once, up front - so the synchronous
        // AspNetMessageBodyGetter serves it from memory instead of blocking a thread-pool thread.
        pipeline.UseBufferedRequestBody();
        action(pipeline);
        app.Add(serviceResolverFactory => new AspNetApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }

    /// <summary>
    /// Applies HTTP-specific configuration to a platform-neutral <see cref="IBenzeneApplicationBuilder"/>.
    /// No-op on any platform other than Azure Functions.
    /// </summary>
    /// <param name="app">The application builder passed to <c>BenzeneStartUp.Configure</c>.</param>
    /// <param name="action">The action that configures the HTTP middleware pipeline.</param>
    /// <returns><paramref name="app"/>, for method chaining.</returns>
    public static IBenzeneApplicationBuilder UseHttp(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<AspNetContext>> action)
    {
        if (app is IAzureFunctionAppBuilder azureApp)
        {
            azureApp.UseHttp(action);
        }
        return app;
    }

    /// <summary>
    /// Registers the services required to process HTTP-triggered messages.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="UseHttp(IAzureFunctionAppBuilder, Action{IMiddlewarePipelineBuilder{AspNetContext}})"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddAspNet(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IMessageTopicGetter<AspNetContext>, AspNetMessageTopicGetter>();
        services.AddScoped<IMessageVersionGetter<AspNetContext>>(resolver =>
            new AspNetMessageVersionGetter(resolver.GetService<IRouteFinder>(),
                resolver.GetService<IMessageHeadersGetter<AspNetContext>>(),
                resolver.TryGetService<MessageVersionHeaderNames>()?.HeaderNames));
        services.AddScoped<IMessageHeadersGetter<AspNetContext>, AspNetMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<AspNetContext>, AspNetMessageBodyGetter>();
        services.AddScoped<IHttpRequestBodyReader<AspNetContext>, AspNetMessageBodyGetter>();
        services.AddScoped<HttpRequestBodyBuffer>();
        services.AddScoped<IMessageHandlerResultSetter<AspNetContext>, AspNetMessageHandlerResultSetter>();
        services.AddScoped<IHttpRequestAdapter<AspNetContext>, AspNetHttpRequestAdapter>();
        services.AddScoped<IBenzeneResponseAdapter<AspNetContext>, AspNetResponseAdapter>();

        services.AddScoped<IResponseRenderer<AspNetContext>, SerializerResponseRenderer<AspNetContext>>();
        services.AddScoped<IResponseHandler<AspNetContext>, RendererResponseHandler<AspNetContext>>();
        services.AddScoped<IResponseHandler<AspNetContext>, HttpStatusCodeResponseHandler<AspNetContext>>();
        services.AddMediaFormatNegotiation<AspNetContext>();

        // services.AddScoped<ResponseMiddleware<AspNetContext>>();
        services.AddScoped<IRequestEnricher<AspNetContext>, AspNetContextRequestEnricher>();

        // Declare this transport for ITransportsInfo (the same name the per-invocation current-transport
        // records), matching Benzene.AspNet.Core - it was previously omitted here.
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.Asp));

        services.AddHttpMessageHandlers();
        return services;
    }
}
