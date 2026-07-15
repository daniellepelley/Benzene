using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Middleware;

/// <summary>
/// Regression coverage for a fix to <see cref="MiddlewareApplication{TEvent,TContext}"/> and
/// <see cref="MiddlewareApplication{TEvent,TContext,TResult}"/>: both create a new DI scope per
/// <c>HandleAsync</c> call via <see cref="IServiceResolverFactory.CreateScope"/>, but previously
/// never disposed it - every event/message processed through either overload (used by
/// <c>AspNetApplication</c>, <c>KafkaApplication</c>, <c>HttpListenerApplication</c>,
/// <c>BenzeneMessageApplication</c>, and anything built on them) leaked a scope, and any scoped
/// <see cref="IDisposable"/> resolved inside it, for the lifetime of the process. Combined with
/// <see cref="Benzene.Test.Core.Core.DI.ServiceResolverScopeDisposalTest"/> (which fixed the
/// adapters' own <c>Dispose()</c> being a no-op), this closes the loop end to end: the scope is
/// both disposed here, and disposing it now actually releases scoped services.
/// </summary>
public class MiddlewareApplicationScopeDisposalTest
{
    private class DisposableTracker : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }

    private class ResolvingPipeline<TContext> : IMiddlewarePipeline<TContext>
    {
        public DisposableTracker ResolvedTracker { get; private set; }

        public Task HandleAsync(TContext context, IServiceResolver serviceResolver)
        {
            ResolvedTracker = serviceResolver.GetService<DisposableTracker>();
            return Task.CompletedTask;
        }
    }

    private static IServiceResolverFactory CreateFactory()
    {
        var services = new ServiceCollection();
        services.AddScoped<DisposableTracker>();
        return new MicrosoftServiceResolverFactory(services);
    }

    [Fact]
    public async Task MiddlewareApplication_NoResult_DisposesTheScopeAfterHandleAsync()
    {
        var pipeline = new ResolvingPipeline<string>();
        var application = new MiddlewareApplication<string, string>(pipeline, @event => @event);

        await application.HandleAsync("event", CreateFactory());

        Assert.NotNull(pipeline.ResolvedTracker);
        Assert.True(pipeline.ResolvedTracker.Disposed);
    }

    [Fact]
    public async Task MiddlewareApplication_WithResult_DisposesTheScopeAfterHandleAsync()
    {
        var pipeline = new ResolvingPipeline<string>();
        var application = new MiddlewareApplication<string, string, string>(pipeline, @event => @event, context => context);

        await application.HandleAsync("event", CreateFactory());

        Assert.NotNull(pipeline.ResolvedTracker);
        Assert.True(pipeline.ResolvedTracker.Disposed);
    }
}
