using System;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Middleware;

/// <summary>
/// Verifies that <see cref="MiddlewareMultiApplication{TEvent,TContext}"/> honors its opt-in
/// <c>maxDegreeOfParallelism</c> - the knob every batch fan-out transport inherits (directly, or via
/// its own options object) from this shared primitive. An unset cap keeps the original unbounded
/// fan-out; a positive cap bounds how many records run at once.
/// </summary>
public class MiddlewareMultiApplicationConcurrencyTest
{
    private sealed class ProbePipeline<TContext> : IMiddlewarePipeline<TContext>
    {
        private readonly object _gate = new();
        private int _current;
        public int MaxObserved { get; private set; }

        public async Task HandleAsync(TContext context, IServiceResolver serviceResolver)
        {
            lock (_gate)
            {
                _current++;
                MaxObserved = Math.Max(MaxObserved, _current);
            }

            await Task.Delay(50);

            lock (_gate)
            {
                _current--;
            }
        }
    }

    private static IServiceResolverFactory CreateFactory()
        => new MicrosoftServiceResolverFactory(new ServiceCollection());

    [Fact]
    public async Task HandleAsync_WithMaxDegreeOfParallelism_NeverRunsMoreThanThatManyAtOnce()
    {
        var pipeline = new ProbePipeline<int>();
        var app = new MiddlewareMultiApplication<int, int>(
            pipeline,
            count => Enumerable.Range(0, count).ToArray(),
            maxDegreeOfParallelism: 3);

        await app.HandleAsync(30, CreateFactory());

        Assert.True(pipeline.MaxObserved <= 3, $"Expected at most 3 concurrent, observed {pipeline.MaxObserved}.");
        Assert.Equal(3, pipeline.MaxObserved);
    }

    [Fact]
    public async Task HandleAsync_WithNoCap_FansOutEveryRecordAtOnce()
    {
        var pipeline = new ProbePipeline<int>();
        var app = new MiddlewareMultiApplication<int, int>(
            pipeline,
            count => System.Linq.Enumerable.Range(0, count).ToArray());

        await app.HandleAsync(25, CreateFactory());

        Assert.Equal(25, pipeline.MaxObserved);
    }
}
