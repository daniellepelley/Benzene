using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Response;
using Benzene.Core.DI;
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

        services.TryAddScoped<IMessageTopicMapper<HttpContext>, HttpMessageTopicMapper>();
        services.TryAddScoped<IMessageHeadersMapper<HttpContext>, HttpMessageHeadersMapper>();
        services.TryAddScoped<IMessageBodyMapper<HttpContext>, HttpMessageBodyMapper>();
        services
            .AddScoped<IRequestMapper<HttpContext>,
                MultiSerializerOptionsRequestMapper<HttpContext, JsonSerializer>>();
        services.AddScoped<IRequestEnricher<HttpContext>, HttpRequestEnricher>();
        services.TryAddScoped<IHttpHeaderMappings, DefaultHttpHeaderMappings>();
        services.AddScoped<IResponseHandler<HttpContext>, HttpStatusCodeResponseHandler<HttpContext>>();
        services
            .AddScoped<IResponseHandler<HttpContext>,
                ResponseHandler<JsonSerializationResponseHandler<HttpContext>, HttpContext>>();
        services.AddScoped<IBenzeneResponseAdapter<HttpContext>, HttpContextResponseAdapter>();
        
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("http"));
        services.AddHttpMessageHandlers();

        return services;
    }


}
