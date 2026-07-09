using System.Threading.Tasks;
using Benzene.Core.Middleware;
using Benzene.Diagnostics.Timers;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Diagnostics;

public class UseTimerTest
{
    [Fact]
    public async Task Timer_ExecutionTime()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);

        long time = -1;
        builder.UseTimer<object>((_, t) => time = t);
        builder.Use(async (_, next) =>
        {
            await Task.Delay(10);
            await next();
        });

        var pipeline = builder.Build();

        using var factory = new MicrosoftServiceResolverFactory(services);
        using var resolver = factory.CreateScope();

        await pipeline.HandleAsync(new object(), resolver);

        Assert.True(time >= 0);
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
