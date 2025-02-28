using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Confluent.Kafka;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Kafka.Core.Kafka;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<KafkaSendMessageContext> UseKafkaClient(
        this IMiddlewarePipelineBuilder<KafkaSendMessageContext> app, IProducer<string, string> producer)
    {
        return app.Use(_ => new KafkaClientMiddleware(producer));
    }
 
    public static IMiddlewarePipelineBuilder<KafkaSendMessageContext> UseKafkaClient(
        this IMiddlewarePipelineBuilder<KafkaSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<KafkaClientMiddleware>());
        return app.Use<KafkaSendMessageContext, KafkaClientMiddleware>();
    }
   
    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseKafka<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app,
        Action<IMiddlewarePipelineBuilder<KafkaSendMessageContext>> action)
    {
        return Convert(app, new KafkaContextConverter<T>(), action);
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> UseKafka<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>> app)
    {
        return app.Convert(new KafkaContextConverter<T>(), builder => builder.UseKafkaClient());
    }
}