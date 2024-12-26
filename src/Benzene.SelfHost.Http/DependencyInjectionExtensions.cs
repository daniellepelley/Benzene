using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.Info;
using Benzene.Core.Request;
using Benzene.Core.Response;
using Benzene.Core.Serialization;
using Benzene.Http;

namespace Benzene.SelfHost.Http;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddHttp(this IBenzeneServiceContainer services)
    {
        services.TryAddScoped<JsonSerializer>();

        services.TryAddScoped<IMessageTopicMapper<SelfHostHttpContext>, HttpListenerMessageTopicMapper>();
        services.TryAddScoped<IMessageHeadersMapper<SelfHostHttpContext>, HttpListenerMessageHeadersMapper>();
        services.TryAddScoped<IMessageBodyMapper<SelfHostHttpContext>, HttpListenerMessageBodyMapper>();
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
