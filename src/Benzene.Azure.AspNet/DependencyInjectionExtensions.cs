using Benzene.Abstractions.DI;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Request;
using Benzene.Abstractions.Response;
using Benzene.Aws.ApiGateway;
using Benzene.Azure.Core;
using Benzene.Core.Response;
using Benzene.Http;

namespace Benzene.Azure.AspNet;

public static class DependencyInjectionExtensions
{
    public static IAzureFunctionAppBuilder UseHttp(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<AspNetContext>> action)
    {
        app.Register(x => x.AddAspNet());
        var pipeline = app.Create<AspNetContext>();
        action(pipeline);
        app.Add(serviceResolverFactory => new AspNetApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }
    
    public static IBenzeneServiceContainer AddAspNet(this IBenzeneServiceContainer services)
    {
        services.AddScoped<IMessageTopicMapper<AspNetContext>, AspNetMessageTopicMapper>();
        services.AddScoped<IMessageHeadersMapper<AspNetContext>, AspNetMessageHeadersMapper>();
        services.AddScoped<IMessageBodyMapper<AspNetContext>, AspNetMessageBodyMapper>();
        services.AddScoped<IHttpRequestAdapter<AspNetContext>, AspNetHttpRequestAdapter>();
        services.AddScoped<IBenzeneResponseAdapter<AspNetContext>, AspNetResponseAdapter>();

        services.AddScoped<IResponseHandler<AspNetContext>, ResponseBodyHandler<AspNetContext>>();
        services.AddScoped<IResponseHandler<AspNetContext>, HttpStatusCodeResponseHandler<AspNetContext>>();

        services.AddScoped<ResponseMiddleware<AspNetContext>>();
        services.AddScoped<IRequestEnricher<AspNetContext>, AspNetContextRequestEnricher>();
        services.AddHttpMessageHandlers();
        return services;
    }

}
