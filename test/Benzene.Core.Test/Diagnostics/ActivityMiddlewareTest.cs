using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Diagnostics.Timers;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Diagnostics;

public class ActivityMiddlewareTest
{
    private static (List<Activity> Activities, ActivityListener Listener) ListenToBenzeneActivities()
    {
        var activities = new List<Activity>();
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BenzeneDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);
        return (activities, listener);
    }

    [Fact]
    public async Task AddDiagnostics_WrapsEveryMiddlewareInAnActivity()
    {
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddDiagnostics();

        var builder = new MiddlewarePipelineBuilder<object>(container);
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        Assert.Contains(activities, a => a.Source.Name == BenzeneDiagnostics.SourceName);
    }

    [Fact]
    public async Task AddActivityPerMiddleware_ProducesOneNamedSpanPerMiddleware()
    {
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        // Focused opt-in only — no debug wrapper, timer factory, or correlation.
        container.AddActivityPerMiddleware();

        var builder = new MiddlewarePipelineBuilder<object>(container);
        builder.Use("first", (_, next) => next());
        builder.Use("second", (_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        // Each middleware turns up as its own span, named after the middleware.
        Assert.Contains(activities, a => a.OperationName == "first");
        Assert.Contains(activities, a => a.OperationName == "second");
    }

    [Fact]
    public void AddActivityPerMiddleware_IsIdempotentAndComposesWithAddDiagnostics()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        container.AddActivityPerMiddleware();
        container.AddDiagnostics();
        container.AddActivityPerMiddleware();

        // The registration guard means the wrapper is only ever registered once as an
        // IMiddlewareWrapper, so a middleware is never double-wrapped no matter how the two
        // opt-ins are combined (the factory resolves IEnumerable<IMiddlewareWrapper>).
        Assert.Single(services.Where(d =>
            d.ServiceType == typeof(IMiddlewareWrapper) &&
            d.ImplementationType == typeof(ActivityMiddlewareWrapper)));
    }

    [Fact]
    public async Task AddDiagnostics_OmitsTheTransportTagWhileTheTransportIsUnresolved()
    {
        // In a multi-transport function the outer probe stages run before any transport pipeline has
        // recorded itself, so ICurrentTransport still reads TransportNames.Unresolved ("<missing>").
        // The span must NOT be tagged then - a "<missing>" transport reads like a defect in a trace viewer.
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddDiagnostics();
        services.AddScoped<CurrentTransportInfo>();
        services.AddScoped<ICurrentTransport>(sp => sp.GetRequiredService<CurrentTransportInfo>());

        var builder = new MiddlewarePipelineBuilder<object>(container);
        builder.Use("probe", (_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        var span = Assert.Single(activities, a => a.OperationName == "probe");
        Assert.Null(span.GetTagItem("benzene.transport"));
    }

    [Fact]
    public async Task AddDiagnostics_TagsTheTransportOnceATransportPipelineHasResolvedIt()
    {
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddDiagnostics();
        services.AddScoped<CurrentTransportInfo>();
        services.AddScoped<ICurrentTransport>(sp => sp.GetRequiredService<CurrentTransportInfo>());

        var builder = new MiddlewarePipelineBuilder<object>(container);
        builder.Use("resolved", (_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        // Stand in for a transport pipeline recording itself before the stage runs.
        resolver.GetService<CurrentTransportInfo>().SetTransport(TransportNames.Sns);

        await pipeline.HandleAsync(new object(), resolver);

        var span = Assert.Single(activities, a => a.OperationName == "resolved");
        Assert.Equal(TransportNames.Sns, span.GetTagItem("benzene.transport"));
    }

    [Fact]
    public async Task AddDiagnostics_MarksTheSpanAsErrorWhenAMiddlewareThrows()
    {
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddDiagnostics();

        var builder = new MiddlewarePipelineBuilder<object>(container);
        builder.Use("boom", (_, _) => throw new InvalidOperationException("kaboom"));

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.HandleAsync(new object(), resolver));

        // Without the fix a thrown span looks identical to a successful one - it must carry Error
        // status and an exception event so a trace viewer can point at the failing stage.
        Assert.Contains(activities, a =>
            a.Status == ActivityStatusCode.Error && a.Events.Any(e => e.Name == "exception"));
    }

    private class StatusContext : Benzene.Abstractions.MessageHandlers.IHasMessageResult
    {
        public Benzene.Abstractions.Results.IBenzeneResult MessageResult { get; set; } =
            Benzene.Results.BenzeneResult.Ok();
    }

    // Resolves a real topic (so the span is topic-bearing, the one that carries benzene.status), and
    // returns whatever headers it was given (empty by default) so the correlation-id tag can be exercised.
    private class FakeMessageGetter : Benzene.Abstractions.MessageHandlers.Mappers.IMessageGetter<StatusContext>
    {
        public System.Collections.Generic.IDictionary<string, string> Headers { get; init; } =
            new System.Collections.Generic.Dictionary<string, string>();
        public Benzene.Abstractions.Messages.ITopic? GetTopic(StatusContext context) =>
            new Benzene.Core.Messages.Topic("orders:create");
        public string? GetBody(StatusContext context) => null;
        public System.Collections.Generic.IDictionary<string, string> GetHeaders(StatusContext context) => Headers;
    }

    [Fact]
    public async Task AddDiagnostics_TagsBenzeneStatusOnTheTopicBearingSpan_FromTheResult()
    {
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddDiagnostics();
        services.AddScoped<Benzene.Abstractions.MessageHandlers.Mappers.IMessageGetter<StatusContext>, FakeMessageGetter>();

        var builder = new MiddlewarePipelineBuilder<StatusContext>(container);
        builder.Use("handle", (_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        // A trace-backed mesh reader reconstructs MeshTraceEvent.Status from this tag - the real wire
        // status, on the same span it reads benzene.topic off.
        await pipeline.HandleAsync(new StatusContext { MessageResult = Benzene.Results.BenzeneResult.NotFound() }, resolver);

        var span = Assert.Single(activities, a => a.OperationName == "handle");
        Assert.Equal("orders:create", span.GetTagItem("benzene.topic"));
        Assert.Equal("not-found", span.GetTagItem("benzene.status"));
    }

    [Fact]
    public async Task AddDiagnostics_StampsTheIdentityTagsOnASingleSpan_NotEveryTopicResolvingStage()
    {
        // For a transport whose topic is intrinsic to the message (HTTP route / BenzeneMessage
        // envelope), GetTopic resolves at EVERY stage - here the FakeMessageGetter always returns a
        // topic. Every middleware must still get its own span, but the message-identity tags
        // (topic/version/status/correlation) must land on exactly ONE span, otherwise a trace-backed
        // mesh reader emits one flow event per middleware stage and over-counts the message.
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddDiagnostics();
        services.AddScoped<Benzene.Abstractions.MessageHandlers.Mappers.IMessageGetter<StatusContext>, FakeMessageGetter>();

        var builder = new MiddlewarePipelineBuilder<StatusContext>(container);
        builder.Use("first", (_, next) => next());
        builder.Use("second", (_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new StatusContext { MessageResult = Benzene.Results.BenzeneResult.NotFound() }, resolver);

        // Both middleware still get their own span (span-per-middleware is unchanged)...
        var firstSpan = Assert.Single(activities, a => a.OperationName == "first");
        var secondSpan = Assert.Single(activities, a => a.OperationName == "second");

        // ...but only the first (outermost) topic-bearing span carries the identity tags + status.
        Assert.Equal("orders:create", firstSpan.GetTagItem("benzene.topic"));
        Assert.Equal("not-found", firstSpan.GetTagItem("benzene.status"));
        Assert.Null(secondSpan.GetTagItem("benzene.topic"));
        Assert.Null(secondSpan.GetTagItem("benzene.status"));

        // Exactly one span across the whole trace carries the topic - what the mesh reader counts.
        Assert.Single(activities, a => a.GetTagItem("benzene.topic") is not null);
    }

    [Fact]
    public async Task AddDiagnostics_TagsBenzeneCorrelationId_WhenTheMessageCarriesOne()
    {
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddDiagnostics();
        // A message carrying x-correlation-id → the searchable annotation mesh:query:correlation needs.
        services.AddScoped<Benzene.Abstractions.MessageHandlers.Mappers.IMessageGetter<StatusContext>>(
            _ => new FakeMessageGetter
            {
                Headers = new System.Collections.Generic.Dictionary<string, string> { ["x-correlation-id"] = "ticket-42" }
            });

        var builder = new MiddlewarePipelineBuilder<StatusContext>(container);
        builder.Use("handle", (_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new StatusContext(), resolver);

        var span = Assert.Single(activities, a => a.OperationName == "handle");
        Assert.Equal("ticket-42", span.GetTagItem("benzene.correlation-id"));
    }

    [Fact]
    public async Task AddDiagnostics_OmitsBenzeneCorrelationId_WhenTheMessageHasNone()
    {
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddDiagnostics();
        // No x-correlation-id header → the tag must be absent, never a fabricated/auto-generated id.
        services.AddScoped<Benzene.Abstractions.MessageHandlers.Mappers.IMessageGetter<StatusContext>, FakeMessageGetter>();

        var builder = new MiddlewarePipelineBuilder<StatusContext>(container);
        builder.Use("handle", (_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new StatusContext(), resolver);

        var span = Assert.Single(activities, a => a.OperationName == "handle");
        Assert.Null(span.GetTagItem("benzene.correlation-id"));
    }

    [Fact]
    public async Task AddDiagnostics_TagsBenzeneStatusException_WhenTheTopicBearingSpanThrows()
    {
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        container.AddDiagnostics();
        services.AddScoped<Benzene.Abstractions.MessageHandlers.Mappers.IMessageGetter<StatusContext>, FakeMessageGetter>();

        var builder = new MiddlewarePipelineBuilder<StatusContext>(container);
        builder.Use("handle", (_, _) => throw new InvalidOperationException("kaboom"));

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipeline.HandleAsync(new StatusContext(), resolver));

        var span = Assert.Single(activities, a => a.OperationName == "handle");
        Assert.Equal("exception", span.GetTagItem("benzene.status"));
    }

    [Fact]
    public async Task UseTimer_StillCompilesAndProducesAnActivity()
    {
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddDiagnostics();

        var builder = new MiddlewarePipelineBuilder<object>(container);
        builder.UseTimer("my-timer");
        builder.Use((_, next) => next());

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        Assert.Contains(activities, a => a.OperationName == "my-timer");
    }

    [Fact]
    public async Task UseTimer_MarksTheTimerSpanAsErrorWhenWrappedWorkThrows()
    {
        var (activities, listener) = ListenToBenzeneActivities();
        using var _ = listener;

        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzeneMiddleware();
        // Register only the timer factory, not the full AddDiagnostics per-middleware wrapper, so the
        // timer's own span is the sole "my-timer" activity (AddDiagnostics would also wrap the timer
        // middleware itself in a same-named span, making the assertion ambiguous).
        services.AddSingleton<IProcessTimerFactory, ActivityProcessTimerFactory>();

        var builder = new MiddlewarePipelineBuilder<object>(container);
        builder.UseTimer("my-timer");
        builder.Use("boom", (_, _) => throw new InvalidOperationException("kaboom"));

        var pipeline = builder.Build();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.HandleAsync(new object(), resolver));

        // The timer's Dispose() can't see the throw, so without the wrapper's try/catch the "my-timer"
        // span would end as successful. It must carry Error status and an exception event.
        var timerSpan = Assert.Single(activities, a => a.OperationName == "my-timer");
        Assert.Equal(ActivityStatusCode.Error, timerSpan.Status);
        Assert.Contains(timerSpan.Events, e => e.Name == "exception");
    }
}
