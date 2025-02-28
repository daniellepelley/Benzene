using Benzene.Abstractions.DI;
using Benzene.Core.MessageHandlers;
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
}
