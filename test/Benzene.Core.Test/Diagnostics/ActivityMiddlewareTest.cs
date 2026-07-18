using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Middleware;
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
}
