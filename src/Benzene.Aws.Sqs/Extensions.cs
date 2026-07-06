using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.MessageHandlers.DI;
using Benzene.SelfHost;

namespace Benzene.Aws.Sqs;

public static class Extensions
{
    public static IBenzeneWorkerStartup UseSqs(this IBenzeneWorkerStartup app, SqsConsumerConfig sqsConsumerConfig, ISqsClientFactory sqsClientFactory, Action<IMiddlewarePipelineBuilder<SqsConsumerMessageContext>> action)
    {
        app.Register(x => x
            .AddBenzeneMessage()
            .AddSqsConsumer()
        );
        var middlewarePipelineBuilder = app.Create<SqsConsumerMessageContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        
        var kafkaApplication = new SqsConsumerApplication(pipeline);
        app.Add(serviceResolverFactory => new SqsConsumer(serviceResolverFactory, kafkaApplication, sqsConsumerConfig, sqsClientFactory));
        return app;
    }
}
