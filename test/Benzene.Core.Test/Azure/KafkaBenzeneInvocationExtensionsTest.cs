using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.Kafka;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Azure;

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

        var context = new KafkaContext(new KafkaRecord { Topic = "orders", Partition = 1, Offset = 7 });

        await pipeline.HandleAsync(context, resolver);
        var resolved = resolver.GetService<IBenzeneInvocation>();

        Assert.Equal("orders-1-7", resolved.InvocationId);
        Assert.Equal("AzureFunctions", resolved.Platform);
    }
}
