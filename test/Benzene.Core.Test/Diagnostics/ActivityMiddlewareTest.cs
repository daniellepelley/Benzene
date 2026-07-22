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
