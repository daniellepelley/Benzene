using System;
using System.Threading.Tasks;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.MiddlewareBuilder;

public class MiddlewareTest
{
    [Fact]
    public async Task ExceptionHandler_CaughtException()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);

        var caught = false;
        builder.UseExceptionHandler((_, _) => caught = true);
        builder.Use((_, _) => throw new Exception("Test"));

        var pipeline = builder.Build();

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        Assert.True(caught);
    }

    [Fact]
    public void Builder_Clear()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);

        builder.Use((_, _) => Task.CompletedTask);
        Assert.Single(builder.GetItems());

        builder.Clear();
        Assert.Empty(builder.GetItems());
    }
}
