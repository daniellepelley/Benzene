using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Diagnostics;

public class BenzeneMetricsTest
{
    private class FakeMessageResult : IMessageResult
    {
        public bool IsSuccessful { get; init; }
    }

    private class FakeMessageContext : IHasMessageResult
    {
        public IMessageResult MessageResult { get; set; } = new FakeMessageResult { IsSuccessful = true };
    }

    private static (List<(string Name, IReadOnlyList<KeyValuePair<string, object>> Tags)> LongMeasurements,
        List<(string Name, IReadOnlyList<KeyValuePair<string, object>> Tags)> DoubleMeasurements,
        MeterListener Listener) ListenToBenzeneMeter()
    {
        var longMeasurements = new List<(string, IReadOnlyList<KeyValuePair<string, object>>)>();
        var doubleMeasurements = new List<(string, IReadOnlyList<KeyValuePair<string, object>>)>();

        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == BenzeneDiagnostics.SourceName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            longMeasurements.Add((instrument.Name, tags.ToArray())));
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
            doubleMeasurements.Add((instrument.Name, tags.ToArray())));
        listener.Start();

        return (longMeasurements, doubleMeasurements, listener);
    }

    [Fact]
    public async Task UseBenzeneMetrics_RecordsCounterAndHistogram()
    {
        var (longMeasurements, doubleMeasurements, listener) = ListenToBenzeneMeter();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();

        var builder = new MiddlewarePipelineBuilder<FakeMessageContext>(container);
        builder.UseBenzeneMetrics();
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new FakeMessageContext(), resolver);

        var count = Assert.Single(longMeasurements, m => m.Name == "benzene.messages.processed");
        Assert.Contains(count.Tags, t => t.Key == "result" && (string)t.Value == "success");
        Assert.Contains(count.Tags, t => t.Key == "topic" && (string)t.Value == "<missing>");
        Assert.Contains(count.Tags, t => t.Key == "transport" && (string)t.Value == "<missing>");

        var duration = Assert.Single(doubleMeasurements, m => m.Name == "benzene.message.duration");
        Assert.Contains(duration.Tags, t => t.Key == "result" && (string)t.Value == "success");
    }

    [Fact]
    public async Task UseBenzeneMetrics_WhenThePipelineThrows_StillRecordsAsFailure()
    {
        var (longMeasurements, doubleMeasurements, listener) = ListenToBenzeneMeter();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();

        var builder = new MiddlewarePipelineBuilder<FakeMessageContext>(container);
        builder.UseBenzeneMetrics();
        builder.Use((_, _) => throw new InvalidOperationException("boom"));

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        // The exception must still propagate...
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.HandleAsync(new FakeMessageContext(), resolver));

        // ...and the message must still be counted and timed, tagged result=failure. Previously a
        // throwing pipeline recorded nothing at all, so failing requests never appeared in the metrics.
        var count = Assert.Single(longMeasurements, m => m.Name == "benzene.messages.processed");
        Assert.Contains(count.Tags, t => t.Key == "result" && (string)t.Value == "failure");
        Assert.Single(doubleMeasurements, m => m.Name == "benzene.message.duration");
    }

    [Fact]
    public async Task UseBenzeneMetrics_NullMessageResult_RecordsMissingWithoutThrowing()
    {
        var (longMeasurements, _, listener) = ListenToBenzeneMeter();
        using var _l = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();

        var builder = new MiddlewarePipelineBuilder<FakeMessageContext>(container);
        builder.UseBenzeneMetrics();
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        // A non-throwing completion that left MessageResult null must not NullReferenceException in
        // the recording finally; the unknown outcome is tagged "<missing>", like a context that
        // carries no result signal at all.
        await pipeline.HandleAsync(new FakeMessageContext { MessageResult = null! }, resolver);

        var count = Assert.Single(longMeasurements, m => m.Name == "benzene.messages.processed");
        Assert.Contains(count.Tags, t => t.Key == "result" && (string)t.Value == "<missing>");
    }
}
