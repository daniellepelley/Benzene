using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS.Model;
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

public class SqsBenzeneInvocationExtensionsTest
{
    [Fact]
    public async Task LambdaUseBenzeneInvocation_SetsInvocationIdToSqsMessageId()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        var builder = new MiddlewarePipelineBuilder<SqsMessageContext>(container);
        builder.UseBenzeneInvocation();
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        var sqsMessage = new SQSEvent.SQSMessage { MessageId = "msg-123" };
        var context = SqsMessageContext.CreateInstance(new SQSEvent(), sqsMessage);

        await pipeline.HandleAsync(context, resolver);
        var resolved = resolver.GetService<IBenzeneInvocation>();

        Assert.Equal("msg-123", resolved.InvocationId);
        Assert.Equal("AwsLambda", resolved.Platform);
    }

    [Fact]
    public async Task SelfHostedUseBenzeneInvocation_SetsInvocationIdToSqsMessageId()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        var builder = new MiddlewarePipelineBuilder<SqsConsumerMessageContext>(container);
        builder.UseBenzeneInvocation();
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        var context = SqsConsumerMessageContext.CreateInstance(new Message { MessageId = "worker-msg-456" });

        await pipeline.HandleAsync(context, resolver);
        var resolved = resolver.GetService<IBenzeneInvocation>();

        Assert.Equal("worker-msg-456", resolved.InvocationId);
        Assert.Equal("Worker", resolved.Platform);
    }

    [Fact]
    public async Task NestedPerMessageScope_ResolvesItsOwnInvocation_NotTheOuterOnesOrNull()
    {
        // Regression test for the exact bug shape Tier 3.5 fixes: SqsApplication (and every other
        // batch transport) dispatches each record through its own fresh DI scope
        // (serviceResolverFactory.CreateScope()), disconnected from whatever IBenzeneInvocation an
        // outer Lambda-invocation-level pipeline populated. Before the fix, resolving
        // IBenzeneInvocation inside that fresh scope either threw (via GetService) or silently came
        // back null (via TryGetService, swallowing the BenzeneException) - see
        // Benzene.Diagnostics.EnrichmentExtensions.UseBenzeneEnrichment's invocationId enrichment.
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        // The "outer" invocation-level pipeline (simulating AwsEventStreamContext-level UseBenzeneInvocation()).
        var outerBuilder = new MiddlewarePipelineBuilder<object>(container);
        outerBuilder.UseBenzeneInvocation((_, _) =>
            new BenzeneInvocation("outer-lambda-request-id", "AwsLambda", new Dictionary<System.Type, object>()));
        outerBuilder.Use((_, next) => next());
        var outerPipeline = outerBuilder.Build();

        // The "inner" per-message pipeline SqsApplication actually runs, with the Tier 3.5 fix applied.
        var innerBuilder = new MiddlewarePipelineBuilder<SqsMessageContext>(container);
        innerBuilder.UseBenzeneInvocation();
        innerBuilder.Use((_, next) => next());
        var innerPipeline = innerBuilder.Build();

        using var factory = new MicrosoftServiceResolverFactory(services);

        // Outer scope: populated by the outer pipeline.
        using (var outerResolver = factory.CreateScope())
        {
            await outerPipeline.HandleAsync(new object(), outerResolver);
            Assert.Equal("outer-lambda-request-id", outerResolver.GetService<IBenzeneInvocation>().InvocationId);
        }

        // Inner scope: a brand-new scope, exactly like SqsApplication.HandleAsync creates per
        // record - never touched by the outer pipeline above. Before the fix, this would throw;
        // now it resolves the message's own invocation.
        using (var innerResolver = factory.CreateScope())
        {
            var sqsMessage = new SQSEvent.SQSMessage { MessageId = "inner-record-id" };
            var context = SqsMessageContext.CreateInstance(new SQSEvent(), sqsMessage);

            await innerPipeline.HandleAsync(context, innerResolver);

            var resolved = innerResolver.GetService<IBenzeneInvocation>();
            Assert.Equal("inner-record-id", resolved.InvocationId);
            Assert.NotEqual("outer-lambda-request-id", resolved.InvocationId);
        }
    }
}
