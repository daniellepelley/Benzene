using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.Core.Middleware;
using Benzene.Kafka.Core.KafkaMessage;
using Benzene.Microsoft.Dependencies;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Kafka;

public class KafkaCoreBenzeneInvocationExtensionsTest
{
    [Fact]
    public async Task UseBenzeneInvocation_SetsInvocationIdToTopicPartitionOffset()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        var builder = new MiddlewarePipelineBuilder<KafkaRecordContext<string, string>>(container);
        builder.UseBenzeneInvocation<string, string>();
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = "hello" },
            TopicPartitionOffset = new TopicPartitionOffset("orders", new Partition(3), new Offset(99))
        };
        var context = new KafkaRecordContext<string, string>(consumeResult);

        await pipeline.HandleAsync(context, resolver);
        var resolved = resolver.GetService<IBenzeneInvocation>();

        Assert.Equal("orders-3-99", resolved.InvocationId);
        Assert.Equal("Worker", resolved.Platform);
    }
}
