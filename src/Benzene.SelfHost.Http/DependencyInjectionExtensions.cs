using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Http;

namespace Benzene.SelfHost.Http;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddHttp(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.TryAddScoped<IMessageTopicGetter<SelfHostHttpContext>, HttpListenerMessageTopicGetter>();
        services.TryAddScoped<IMessageVersionGetter<SelfHostHttpContext>, HttpListenerMessageVersionGetter>();
        services.TryAddScoped<IMessageHeadersGetter<SelfHostHttpContext>, HttpListenerMessageHeadersGetter>();
        services.TryAddScoped<IMessageBodyGetter<SelfHostHttpContext>, HttpListenerMessageBodyGetter>();
        services.TryAddScoped<IMessageHandlerResultSetter<SelfHostHttpContext>, HttpListenerMessageHandlerResultSetter>();
        // Registers IResponseHandlerContainer<>, IResponsePayloadMapper<>, IMessageGetter<>, etc. -
        // otherwise only available when .UseMessageHandlers()/.AddMessageHandlers() is also called,
        // which isn't guaranteed for an app that only wires health checks via UseHttp().
        services.AddContextItems();
        services
            .AddScoped<IRequestMapper<SelfHostHttpContext>,
                MultiSerializerOptionsRequestMapper<SelfHostHttpContext>>();
        services.AddScoped<IRequestEnricher<SelfHostHttpContext>, HttpListenerRequestEnricher>();
        services.TryAddScoped<IHttpHeaderMappings, DefaultHttpHeaderMappings>();
        services.AddScoped<IHttpRequestAdapter<SelfHostHttpContext>, HttpListenerRequestAdapter>();
        services.AddScoped<IResponseHandler<SelfHostHttpContext>, HttpStatusCodeResponseHandler<SelfHostHttpContext>>();
        services.AddScoped<IResponseRenderer<SelfHostHttpContext>, SerializerResponseRenderer<SelfHostHttpContext>>();
        services.AddScoped<IResponseHandler<SelfHostHttpContext>, RendererResponseHandler<SelfHostHttpContext>>();
        services.AddMediaFormatNegotiation<SelfHostHttpContext>();
        services.AddScoped<IBenzeneResponseAdapter<SelfHostHttpContext>, HttpContextResponseAdapter>();

        services.AddSingleton<ITransportInfo>(_ => new TransportInfo(TransportNames.Http));
        services.AddHttpMessageHandlers();

        return services;
    }


}
