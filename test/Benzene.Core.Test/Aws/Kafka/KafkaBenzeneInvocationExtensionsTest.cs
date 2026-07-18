using System.Threading.Tasks;
using Amazon.Lambda.KafkaEvents;
using Benzene.Abstractions.Hosting;
using Benzene.Aws.Lambda.Kafka;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Aws.Kafka;

public class KafkaBenzeneInvocationExtensionsTest
{
    [Fact]
    public async Task UseBenzeneInvocation_SetsInvocationIdToTopicPartitionOffset()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        var builder = new MiddlewarePipelineBuilder<KafkaContext>(container);
        builder.UseBenzeneInvocation();
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        var kafkaEvent = new KafkaEvent();
        var record = new KafkaEvent.KafkaEventRecord { Topic = "orders", Partition = 2, Offset = 42 };
        var context = new KafkaContext(kafkaEvent, record);

        await pipeline.HandleAsync(context, resolver);
        var resolved = resolver.GetService<IBenzeneInvocation>();

        Assert.Equal("orders-2-42", resolved.InvocationId);
        Assert.Equal("AwsLambda", resolved.Platform);
    }
}
