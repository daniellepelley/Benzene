using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Grpc.Serialization;
using Grpc.Core;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Grpc.Client;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<GrpcSendMessageContext> UseGrpcClient(
        this IMiddlewarePipelineBuilder<GrpcSendMessageContext> app, CallInvoker callInvoker, IGrpcClientRouteRegistry routeRegistry, IGrpcMessageAdapter adapter)
    {
        return app.Use(_ => new GrpcClientMiddleware(callInvoker, routeRegistry, adapter));
    }

    public static IMiddlewarePipelineBuilder<GrpcSendMessageContext> UseGrpcClient(
        this IMiddlewarePipelineBuilder<GrpcSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<GrpcClientMiddleware>());
        return app.Use<GrpcSendMessageContext, GrpcClientMiddleware>();
    }

    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseGrpc<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        Action<IMiddlewarePipelineBuilder<GrpcSendMessageContext>> action)
    {
        return Convert(app, new GrpcContextConverter<T>(), action);
    }

    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseGrpc<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app)
    {
        return app.Convert(new GrpcContextConverter<T>(), builder => builder.UseGrpcClient());
    }
}
