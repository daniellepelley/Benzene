using Benzene.Aws.XRay;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Aws.XRay;

public class UseXRayTracingTest
{
    [Fact]
    public void Disabled_ReturnsSameBuilder_DoesNotRegisterXRay()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);

        var result = builder.UseXRayTracing(false);

        Assert.Same(builder, result);
    }

    [Fact]
    public void Enabled_ReturnsSameBuilder_RegistersXRayForAllServices()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);

        var result = builder.UseXRayTracing(true);

        Assert.Same(builder, result);
    }
}
