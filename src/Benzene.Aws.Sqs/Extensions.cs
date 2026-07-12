using System;
using Benzene.Abstractions.Middleware;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.MessageHandlers.DI;
using Benzene.SelfHost;

namespace Benzene.Aws.Sqs;

/// <summary>
/// Provides extension methods for adding a standalone SQS polling consumer to a Benzene worker.
/// </summary>
/// <remarks>
/// Unlike <c>Benzene.Aws.Lambda.Sqs</c>, which processes SQS messages delivered via a Lambda event
/// source mapping, this package polls SQS directly using <see cref="Consumer.SqsConsumer"/> — intended
/// for long-running workers (e.g. <c>Benzene.HostedService</c>/<c>Benzene.SelfHost</c>) rather than Lambda.
/// </remarks>
public static class Extensions
{
    /// <summary>
    /// Adds an SQS polling consumer to the worker.
    /// </summary>
    /// <param name="app">The worker startup to add the SQS consumer to.</param>
    /// <param name="sqsConsumerConfig">The queue URL and batch size to poll with.</param>
    /// <param name="sqsClientFactory">The factory used to create the underlying <c>IAmazonSQS</c> client.</param>
    /// <param name="action">The action that configures the inner SQS message pipeline.</param>
    /// <returns>The worker startup for method chaining.</returns>
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
