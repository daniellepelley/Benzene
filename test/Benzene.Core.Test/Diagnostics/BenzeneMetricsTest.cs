using System;
using Benzene.Abstractions.Results;
using Benzene.Results;
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
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Diagnostics;

public class BenzeneMetricsTest
{
    private class FakeMessageContext : IHasMessageResult
    {
        public IBenzeneResult MessageResult { get; set; } = BenzeneResult.Ok();
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
    public async Task UseBenzeneMetrics_WhenThePipelineThrows_StillRecordsAsException()
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

        // ...and the message must still be counted and timed, tagged result=exception - its own
        // category, distinct from a handler that *returned* an unsuccessful result (a controlled
        // failure). Previously a throwing pipeline recorded nothing at all.
        var count = Assert.Single(longMeasurements, m => m.Name == "benzene.messages.processed");
        Assert.Contains(count.Tags, t => t.Key == "result" && (string)t.Value == "exception");
        Assert.Single(doubleMeasurements, m => m.Name == "benzene.message.duration");
    }

    [Fact]
    public async Task UseBenzeneMetrics_UnsuccessfulResult_RecordsTheRootCauseStatus_NotJustFailure()
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

        // Failures are itemized by their real status so the mix is diagnosable (a load of NotFound
        // reads very differently from a load of Unauthorized), not collapsed to a single "failure".
        await pipeline.HandleAsync(new FakeMessageContext { MessageResult = BenzeneResult.NotFound<Void>("nope") }, resolver);

        var count = Assert.Single(longMeasurements, m => m.Name == "benzene.messages.processed");
        Assert.Contains(count.Tags, t => t.Key == "result" && (string)t.Value == "not-found");
    }

    [Fact]
    public async Task UseBenzeneMetrics_SuccessfulResultWithFailureClassStatus_RecordsSuccess()
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

        // Success is decided by IsSuccessful (the bool), NOT the status class: a result that stays
        // successful while carrying a failure-class status (e.g. a health check reporting
        // ServiceUnavailable so the body renders) must still record as "success".
        await pipeline.HandleAsync(
            new FakeMessageContext { MessageResult = BenzeneResult.Set<Void>(BenzeneResultStatus.ServiceUnavailable, true) },
            resolver);

        var count = Assert.Single(longMeasurements, m => m.Name == "benzene.messages.processed");
        Assert.Contains(count.Tags, t => t.Key == "result" && (string)t.Value == "success");
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
