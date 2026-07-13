using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Logging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Benzene.Test.Diagnostics;

public class BenzeneEnrichmentTest
{
    private static ActivityListener ListenToBenzeneActivities()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BenzeneDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public async Task UseBenzeneEnrichment_AddsInvocationIdTraceIdSpanIdToLogScope()
    {
        using var listener = ListenToBenzeneActivities();
        var fakeLoggerFactory = new FakeLoggerFactory();

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(fakeLoggerFactory);
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddDiagnostics();

        var builder = new MiddlewarePipelineBuilder<object>(container);
        builder.UseBenzeneInvocation((_, _) =>
            new BenzeneInvocation("test-invocation-id", "Test", new Dictionary<System.Type, object>()));
        builder.UseBenzeneEnrichment();
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        Assert.Contains(fakeLoggerFactory.Collector.ScopeDictionaries,
            x => x.TryGetValue("invocationId", out var v) && (string)v == "test-invocation-id");
        Assert.Contains(fakeLoggerFactory.Collector.ScopeDictionaries, x => x.ContainsKey("traceId"));
        Assert.Contains(fakeLoggerFactory.Collector.ScopeDictionaries, x => x.ContainsKey("spanId"));
    }

    [Fact]
    public async Task UseBenzeneEnrichment_DegradesGracefully_WithoutInvocationRegistered()
    {
        var fakeLoggerFactory = new FakeLoggerFactory();

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(fakeLoggerFactory);
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();

        var builder = new MiddlewarePipelineBuilder<object>(container);
        builder.UseBenzeneEnrichment();
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        Assert.DoesNotContain(fakeLoggerFactory.Collector.ScopeDictionaries, x => x.ContainsKey("invocationId"));
    }
}
