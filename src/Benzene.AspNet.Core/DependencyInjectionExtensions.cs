using Benzene.Abstractions.DI;
using Benzene.Abstractions.Info;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Serialization;
using Benzene.Core.Info;
using Benzene.Core.MessageHandling;
using Benzene.Core.Middleware;
using Benzene.Core.Request;
using Benzene.Core.Response;
using Benzene.Core.Serialization;
using Benzene.Http;

namespace Benzene.AspNet.Core;

public static class DependencyInjectionExtensions
{
    public static IBenzeneServiceContainer AddAspNetMessageHandlers(this IBenzeneServiceContainer services)
    {
        services.AddScoped<ISerializer, JsonSerializer>();
        services.AddScoped<IMiddlewarePipelineBuilder<AspNetContext>, MiddlewarePipelineBuilder<AspNetContext>>();
        services.AddScoped<IMessageTopicMapper<AspNetContext>, AspNetMessageTopicMapper>();
        services.AddScoped<IMessageHeadersMapper<AspNetContext>, AspNetMessageHeadersMapper>();
        services.AddScoped<IMessageBodyMapper<AspNetContext>, AspNetMessageBodyMapper>();
         services.AddScoped<IResponseHandler<AspNetContext>, HttpStatusCodeResponseHandler<AspNetContext>>();
         services.AddScoped<IResponseHandler<AspNetContext>, ResponseBodyHandler<AspNetContext>>();
        services.AddScoped<MessageRouter<AspNetContext>>();
        services
            .AddScoped<IRequestMapper<AspNetContext>,
                MultiSerializerOptionsRequestMapper<AspNetContext, JsonSerializer>>();
        services.AddScoped<IRequestEnricher<AspNetContext>, AspNetRequestEnricher>();
        services.AddScoped<IHttpRequestAdapter<AspNetContext>, AspNetHttpRequestAdapter>();
        services.AddScoped<IBenzeneResponseAdapter<AspNetContext>, AspNetResponseAdapter>();
        services.TryAddScoped<IHttpHeaderMappings, DefaultHttpHeaderMappings>();
        services.AddScoped<ResponseMiddleware<AspNetContext>>();
        
        services.AddSingleton<ITransportInfo>(_ => new TransportInfo("asp"));
        services.AddHttpMessageHandlers();

        return services;
    }
}