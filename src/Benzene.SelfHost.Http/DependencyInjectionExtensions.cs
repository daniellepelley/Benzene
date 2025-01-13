using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.Info;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Request;
using Benzene.Core.Response;
using Benzene.Http;

namespace Benzene.SelfHost.Http;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddHttp(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.TryAddScoped<IMessageTopicGetter<SelfHostHttpContext>, HttpListenerMessageTopicGetter>();
        services.TryAddScoped<IMessageHeadersGetter<SelfHostHttpContext>, HttpListenerMessageHeadersGetter>();
        services.TryAddScoped<IMessageBodyGetter<SelfHostHttpContext>, HttpListenerMessageBodyGetter>();
        services
            .AddScoped<IRequestMapper<SelfHostHttpContext>,
                MultiSerializerOptionsRequestMapper<SelfHostHttpContext, JsonSerializer>>();
        services.AddScoped<IRequestEnricher<SelfHostHttpContext>, HttpListenerRequestEnricher>();
        services.TryAddScoped<IHttpHeaderMappings, DefaultHttpHeaderMappings>();
        services.AddScoped<IHttpRequestAdapter<SelfHostHttpContext>, HttpListenerRequestAdapter>();
        services.AddScoped<IResponseHandler<SelfHostHttpContext>, HttpStatusCodeResponseHandler<SelfHostHttpContext>>();
        services.AddScoped<IResponseHandler<SelfHostHttpContext>,
                ResponseHandler<JsonSerializationResponseHandler<SelfHostHttpContext>, SelfHostHttpContext>>();
        services.AddScoped<IBenzeneResponseAdapter<SelfHostHttpContext>, HttpContextResponseAdapter>();
        
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("http"));
        services.AddHttpMessageHandlers();

        return services;
    }


}
