using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Middleware.BenzeneClient;
using Benzene.Clients.Common;
using Benzene.Core.Middleware;

namespace Benzene.Client.Http;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<HttpSendMessageContext> UseHttpClient(
        this IMiddlewarePipelineBuilder<HttpSendMessageContext> app, HttpClient httpClient)
    {
        return app.Use(_ => new HttpClientMiddleware(httpClient));
    }
 
    public static IMiddlewarePipelineBuilder<HttpSendMessageContext> UseHttpClient(
        this IMiddlewarePipelineBuilder<HttpSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<HttpClientMiddleware>());
        return app.Use<HttpSendMessageContext, HttpClientMiddleware>();
    }
   
     public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, IMiddlewarePipeline<TContextOut> middlewarePipeline)
    {
        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);

        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>> UseHttp<TRequest, TResponse>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>> app,
        string verb, string path, Action<IMiddlewarePipelineBuilder<HttpSendMessageContext>> action)
    {
        return Convert(app, new HttpContextConverter<TRequest, TResponse>(verb, path), action);
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>> UseHttp<TRequest, TResponse>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>> app,
        string verb, string path)
    {
        return app.Convert(new HttpContextConverter<TRequest, TResponse>(verb, path), builder => builder.UseHttpClient());
    }
}