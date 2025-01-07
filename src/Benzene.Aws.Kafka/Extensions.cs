using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Core.AwsEventStream;
using Benzene.Core.Middleware;

namespace Benzene.Aws.Kafka;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<AwsEventStreamContext> UseKafka(this IMiddlewarePipelineBuilder<AwsEventStreamContext> app, Action<IMiddlewarePipelineBuilder<KafkaContext>> action)
    {
        app.Register(x => x.AddKafka());
        var pipeline = app.CreateMiddlewarePipeline(action);
        return app.Use(resolver => new KafkaLambdaHandler(new KafkaApplication(pipeline), resolver));
    }
}
