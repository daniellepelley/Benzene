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
