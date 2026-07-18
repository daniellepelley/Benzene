using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.EventHub.Function;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Azure;

public class EventHubBenzeneInvocationExtensionsTest
{
    [Fact]
    public async Task UseBenzeneInvocation_SetsInvocationIdToSequenceNumber()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        var builder = new MiddlewarePipelineBuilder<EventHubContext>(container);
        builder.UseBenzeneInvocation();
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        var eventData = EventHubsModelFactory.EventData(
            new BinaryData("hello"), null, null, null, sequenceNumber: 54321, offset: 1, enqueuedTime: DateTimeOffset.UtcNow);
        var context = EventHubContext.CreateInstance(eventData);

        await pipeline.HandleAsync(context, resolver);
        var resolved = resolver.GetService<IBenzeneInvocation>();

        Assert.Equal("54321", resolved.InvocationId);
        Assert.Equal("AzureFunctions", resolved.Platform);
    }
}
