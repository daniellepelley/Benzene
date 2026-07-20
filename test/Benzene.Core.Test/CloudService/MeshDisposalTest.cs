using System;
using System.Net.Http;
using System.Threading.Tasks;
using Benzene.Mesh.Wire;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.CloudService;

/// <summary>
/// The mesh singletons (<see cref="HttpMeshTraceExporter"/> and the CloudService announcer) are
/// <see cref="IAsyncDisposable"/> so their background loop/tail-flush can be stopped on shutdown, but
/// a container disposed synchronously (MS DI's <c>ServiceProvider.Dispose()</c>) throws on a service
/// that only implements <see cref="IAsyncDisposable"/>. They also implement <see cref="IDisposable"/>
/// so both disposal paths are safe. These tests lock that in, plus the realization requirement the
/// CloudService wiring relies on (a factory singleton is only disposed once it has been resolved).
/// </summary>
public class MeshDisposalTest
{
    private sealed class SpyExporter : IMeshTraceExporter, IAsyncDisposable, IDisposable
    {
        public bool SyncDisposed { get; private set; }
        public bool AsyncDisposed { get; private set; }
        public void Export(MeshTraceEvent traceEvent) { }
        public void Dispose() => SyncDisposed = true;
        public ValueTask DisposeAsync() { AsyncDisposed = true; return ValueTask.CompletedTask; }
    }

    [Fact]
    public void RealizedExporter_IsDisposedBySyncProviderDispose_WithoutThrowing()
    {
        var spy = new SpyExporter();
        var services = new ServiceCollection();
        services.AddSingleton<IMeshTraceExporter>(_ => spy);
        var provider = services.BuildServiceProvider();
        _ = provider.GetService<IMeshTraceExporter>(); // realize, as the CloudService realize-middleware does

        // Before the IDisposable bridge this threw InvalidOperationException ("only implements
        // IAsyncDisposable. Use DisposeAsync..."); now the container disposes it via the sync path.
        provider.Dispose();

        Assert.True(spy.SyncDisposed);
    }

    [Fact]
    public async Task RealizedExporter_IsDisposedByAsyncProviderDispose()
    {
        var spy = new SpyExporter();
        var services = new ServiceCollection();
        services.AddSingleton<IMeshTraceExporter>(_ => spy);
        var provider = services.BuildServiceProvider();
        _ = provider.GetService<IMeshTraceExporter>();

        await provider.DisposeAsync();

        Assert.True(spy.AsyncDisposed);
    }

    [Fact]
    public void UnrealizedExporter_IsNotDisposed()
    {
        // Documents why the CloudService wiring must resolve the mesh singletons at least once: a
        // factory-registered singleton that nothing ever resolves is never tracked for disposal.
        var spy = new SpyExporter();
        var services = new ServiceCollection();
        services.AddSingleton<IMeshTraceExporter>(_ => spy);
        var provider = services.BuildServiceProvider();

        provider.Dispose();

        Assert.False(spy.SyncDisposed);
        Assert.False(spy.AsyncDisposed);
    }

    [Fact]
    public async Task HttpMeshTraceExporter_SyncDispose_DoesNotThrow_AndIsIdempotentWithDisposeAsync()
    {
        var exporter = new HttpMeshTraceExporter(new HttpClient(), "http://localhost:1/collector");

        exporter.Dispose();            // sync, bounded - must not throw
        await exporter.DisposeAsync(); // idempotent: a later async disposal just awaits the (finished) pump
    }

    [Fact]
    public void RealizedHttpMeshTraceExporter_IsDisposedBySyncProviderDispose_WithoutThrowing()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => new HttpMeshTraceExporter(new HttpClient(), "http://localhost:1/collector"));
        var provider = services.BuildServiceProvider();
        _ = provider.GetService<HttpMeshTraceExporter>();

        // The real type, registered and realized exactly as CloudService does it - sync container
        // disposal must not throw (it did before the IDisposable bridge).
        provider.Dispose();
    }
}
