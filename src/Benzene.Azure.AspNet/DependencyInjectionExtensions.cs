using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Core;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Http;

namespace Benzene.Azure.AspNet;

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
        action(pipeline);
        app.Add(serviceResolverFactory => new AspNetApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }

    /// <summary>
    /// Registers the services required to process HTTP-triggered messages.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="UseHttp"/>; you don't normally need to call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddAspNet(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IMessageTopicGetter<AspNetContext>, AspNetMessageTopicGetter>();
        services.AddScoped<IMessageHeadersGetter<AspNetContext>, AspNetMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<AspNetContext>, AspNetMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<AspNetContext>, AspNetMessageMessageHandlerResultSetter>();
        services.AddScoped<IHttpRequestAdapter<AspNetContext>, AspNetHttpRequestAdapter>();
        services.AddScoped<IBenzeneResponseAdapter<AspNetContext>, AspNetResponseAdapter>();

        services.AddScoped<IResponseHandler<AspNetContext>, ResponseBodyHandler<AspNetContext>>();
        services.AddScoped<IResponseHandler<AspNetContext>, HttpStatusCodeResponseHandler<AspNetContext>>();

        // services.AddScoped<ResponseMiddleware<AspNetContext>>();
        services.AddScoped<IRequestEnricher<AspNetContext>, AspNetContextRequestEnricher>();
        services.AddHttpMessageHandlers();
        return services;
    }
}
