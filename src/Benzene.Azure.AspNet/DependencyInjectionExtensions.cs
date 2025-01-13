using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
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