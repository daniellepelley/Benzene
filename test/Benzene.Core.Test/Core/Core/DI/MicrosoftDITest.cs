using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Exceptions;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

public class MicrosoftDependencyInjectionTest
{
    private sealed class DisposalSpy : IDisposable, IAsyncDisposable
    {
        public bool Disposed { get; private set; }
        public bool DisposedAsync { get; private set; }
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync() { DisposedAsync = true; return ValueTask.CompletedTask; }
    }

    [Fact]
    public void Dispose_ProviderBuiltByFactory_DisposesSingletons()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DisposalSpy>();

        // The factory built the provider (IServiceCollection ctor), so it owns and disposes it - which
        // runs the container's IDisposable singletons. Previously Dispose() was a no-op and they leaked.
        var factory = new MicrosoftServiceResolverFactory(services);
        DisposalSpy spy;
        using (var scope = factory.CreateScope())
        {
            spy = scope.GetService<DisposalSpy>();
        }
        factory.Dispose();

        Assert.True(spy.Disposed);
    }

    [Fact]
    public async Task DisposeAsync_ProviderBuiltByFactory_AsyncDisposesSingletons()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DisposalSpy>();

        var factory = new MicrosoftServiceResolverFactory(services);
        DisposalSpy spy;
        using (var scope = factory.CreateScope())
        {
            spy = scope.GetService<DisposalSpy>();
        }
        await factory.DisposeAsync();

        Assert.True(spy.DisposedAsync);
    }

    [Fact]
    public void Dispose_ExternallySuppliedProvider_IsNotDisposedByTheFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DisposalSpy>();
        var provider = services.BuildServiceProvider();
        var spy = provider.GetService<DisposalSpy>();

        // The factory was handed a provider it did not build (IServiceProvider ctor); disposing the
        // factory must NOT dispose that provider - the caller owns its lifetime.
        var factory = new MicrosoftServiceResolverFactory(provider);
        factory.Dispose();

        Assert.False(spy.Disposed);
        provider.Dispose(); // now the real owner disposes it
        Assert.True(spy.Disposed);
    }
    [Fact]
    public void AddMessageHandlers()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => Mock.Of<IExampleService>());
        services.UsingBenzene(x => x.AddMessageHandlers(typeof(ExampleRequestPayload).Assembly));

        using var factory = new MicrosoftServiceResolverFactory(services);

        using var serviceResolver = factory.CreateScope();

        var handler = serviceResolver.GetService<ExampleMessageHandler>();
        Assert.NotNull(handler);

        var tryHandler = serviceResolver.TryGetService<ExampleMessageHandler>();
        Assert.NotNull(tryHandler);

        var tryFail = serviceResolver.TryGetService<ExampleRequestPayload>();
        Assert.Null(tryFail);
    }

    [Fact]
    public void AddServiceResolver()
    {
        var services = new ServiceCollection();

        using var factory = new MicrosoftServiceResolverFactory(services);

        using var serviceResolver = factory.CreateScope();

        var serviceResolver2 = serviceResolver.GetService<IServiceResolver>();
        Assert.NotNull(serviceResolver2);
    }

    [Fact]
    public void TryGetService_BuiltInTypes_ResolveSymmetricallyWithGetService()
    {
        var services = new ServiceCollection();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        // TryGetService must special-case the built-in types the same way GetService does (the two
        // used to diverge - GetService handled IServiceResolverFactory, TryGetService didn't).
        Assert.NotNull(serviceResolver.TryGetService<IServiceResolver>());
        Assert.NotNull(serviceResolver.TryGetService<IServiceResolverFactory>());
        Assert.NotNull(serviceResolver.GetService<IServiceResolverFactory>());
    }

    [Fact]
    public void GetService_Unregistered_ThrowsBenzeneException_WithHint_PreservingTheOriginalError()
    {
        var services = new ServiceCollection();
        using var factory = new MicrosoftServiceResolverFactory(services);
        using var serviceResolver = factory.CreateScope();

        var ex = Assert.Throws<BenzeneException>(() => serviceResolver.GetService<IMiddlewareFactory>());

        // The container's real error is preserved (never masked by the diagnostic), and the message
        // carries the actionable registration hint derived from the requested type itself.
        Assert.NotNull(ex.InnerException);
        Assert.Contains("IMiddlewareFactory", ex.Message);
        Assert.Contains(".UsingBenzene(x => x.AddBenzene())", ex.Message);
    }
}
