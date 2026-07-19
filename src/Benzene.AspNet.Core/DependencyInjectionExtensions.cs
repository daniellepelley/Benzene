using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Middleware;
using Benzene.Http;

namespace Benzene.AspNet.Core;

/// <summary>
/// Provides extension methods for registering the services required to process HTTP requests through
/// Benzene's message handler pipeline in ASP.NET Core.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers the services required to route HTTP requests to message handlers.
    /// </summary>
    /// <param name="services">The service container to register services with.</param>
    /// <returns>The service container, for method chaining.</returns>
    /// <remarks>
    /// Called automatically by <see cref="BenzeneExtensions.UseHttp(IAspApplicationBuilder, Action{IMiddlewarePipelineBuilder{AspNetContext}})"/>; you don't normally need to
    /// call this directly.
    /// </remarks>
    public static IBenzeneServiceContainer AddAspNetMessageHandlers(this IBenzeneServiceContainer services)
    {
        services.AddScoped<ISerializer, JsonSerializer>();
        services.AddScoped<IMiddlewarePipelineBuilder<AspNetContext>, MiddlewarePipelineBuilder<AspNetContext>>();
        services.AddScoped<IMessageTopicGetter<AspNetContext>, AspNetMessageTopicGetter>();
        services.AddScoped<IMessageVersionGetter<AspNetContext>, AspNetMessageVersionGetter>();
        services.AddScoped<IMessageHeadersGetter<AspNetContext>, AspNetMessageHeadersGetter>();
        services.AddScoped<IMessageBodyGetter<AspNetContext>, AspNetMessageBodyGetter>();
        services.AddScoped<IMessageHandlerResultSetter<AspNetContext>, AspMessageHandlerResultSetter>();
        services.AddScoped<IResponseHandler<AspNetContext>, HttpStatusCodeResponseHandler<AspNetContext>>();
        services.AddScoped<IResponseRenderer<AspNetContext>, SerializerResponseRenderer<AspNetContext>>();
        services.AddScoped<IResponseHandler<AspNetContext>, RendererResponseHandler<AspNetContext>>();
        services.AddScoped<MessageRouter<AspNetContext>>();
        services.AddMediaFormatNegotiation<AspNetContext>();
        services
            .AddScoped<IRequestMapper<AspNetContext>,
                MultiSerializerOptionsRequestMapper<AspNetContext>>();
        services.AddScoped<IRequestEnricher<AspNetContext>, AspNetRequestEnricher>();
        services.AddScoped<IHttpRequestAdapter<AspNetContext>, AspNetHttpRequestAdapter>();
        services.AddScoped<IBenzeneResponseAdapter<AspNetContext>, AspNetResponseAdapter>();
        services.TryAddScoped<IHttpHeaderMappings, DefaultHttpHeaderMappings>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.Asp));
        services.AddHttpMessageHandlers();

        return services;
    }
}
