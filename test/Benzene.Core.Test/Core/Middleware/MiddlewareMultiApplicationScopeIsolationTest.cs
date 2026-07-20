using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Middleware;

/// <summary>
/// Concurrency regression coverage for the scope-granularity contract that batch processing depends
/// on: <see cref="MiddlewareMultiApplication{TEvent,TContext}"/> must create one DI scope PER record,
/// not one scope shared across the whole batch. Sharing a scope across records dispatched
/// concurrently is the classic production bug this framework has to defend against - e.g. a scoped
/// Entity Framework DbContext being used from several batch items at once (DbContext is not
/// thread-safe), causing "a second operation started on this context" failures and connection
/// contention under load.
///
/// The tests below resolve a scoped marker inside a deliberately-yielding pipeline (so the record
/// continuations genuinely overlap on pool threads) and assert every record saw its OWN marker
/// instance, repeated across many runs.
/// </summary>
public class MiddlewareMultiApplicationScopeIsolationTest
{
    private sealed class ScopeMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    // Resolves the scoped marker for each record after a yield, so the per-record continuations run
    // concurrently on the thread pool - the exact condition under which a shared scope would hand the
    // same scoped instance to several records at once.
    private sealed class YieldingResolvingPipeline<TContext> : IMiddlewarePipeline<TContext>
    {
        private readonly Func<TContext, ScopeMarker, TContext> _record;

        public YieldingResolvingPipeline(Func<TContext, ScopeMarker, TContext> record)
        {
            _record = record;
        }

        public async Task HandleAsync(TContext context, IServiceResolver serviceResolver)
        {
            await Task.Yield();
            var marker = serviceResolver.GetService<ScopeMarker>();
            await Task.Yield();
            // Resolve a second time within the same scope: the SAME scope must return the SAME
            // instance, so a per-record scope yields exactly one distinct marker per record.
            var markerAgain = serviceResolver.GetService<ScopeMarker>();
            Assert.Same(marker, markerAgain);
            _record(context, marker);
        }
    }

    private sealed class Record
    {
        public int Index { get; init; }
        public ScopeMarker Marker { get; set; }
    }

    private static IServiceResolverFactory CreateFactory()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopeMarker>();
        return new MicrosoftServiceResolverFactory(services);
    }

    [Fact]
    public async Task HandleAsync_ManyRecordsConcurrently_EachRecordGetsItsOwnScope()
    {
        const int recordCount = 200;

        for (var run = 0; run < 10; run++)
        {
            var seen = new ConcurrentBag<ScopeMarker>();

            var pipeline = new YieldingResolvingPipeline<Record>((record, marker) =>
            {
                record.Marker = marker;
                seen.Add(marker);
                return record;
            });

            var app = new MiddlewareMultiApplication<int, Record>(
                pipeline,
                count => Enumerable.Range(0, count).Select(i => new Record { Index = i }).ToArray());

            await app.HandleAsync(recordCount, CreateFactory());

            // Every record resolved a marker, and all of them are distinct instances - i.e. every
            // record ran in its own scope. A single shared batch scope would have handed the same
            // instance to every record, collapsing the distinct count to 1.
            var distinctMarkers = seen.Select(m => m.Id).Distinct().Count();
            Assert.Equal(recordCount, seen.Count);
            Assert.Equal(recordCount, distinctMarkers);
        }
    }

    [Fact]
    public async Task HandleAsync_WithResultMapper_ManyRecordsConcurrently_EachRecordGetsItsOwnScope()
    {
        const int recordCount = 200;

        for (var run = 0; run < 10; run++)
        {
            var pipeline = new YieldingResolvingPipeline<Record>((record, marker) =>
            {
                record.Marker = marker;
                return record;
            });

            var app = new MiddlewareMultiApplication<int, Record, Guid>(
                pipeline,
                count => Enumerable.Range(0, count).Select(i => new Record { Index = i }).ToArray(),
                record => record.Marker.Id);

            var markerIds = await app.HandleAsync(recordCount, CreateFactory());

            // The result mapper projects each record's scoped-marker id; all distinct means every
            // record was isolated in its own scope even though they were dispatched concurrently.
            Assert.Equal(recordCount, markerIds.Length);
            Assert.Equal(recordCount, markerIds.Distinct().Count());
        }
    }
}
