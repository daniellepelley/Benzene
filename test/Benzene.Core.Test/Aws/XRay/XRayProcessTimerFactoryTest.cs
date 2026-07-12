using Benzene.Aws.XRay;
using Benzene.Diagnostics.Timers;
using Xunit;

namespace Benzene.Test.Aws.XRay;

public class XRayProcessTimerFactoryTest
{
    [Fact]
    public void Create_ReturnsXRayProcessProcessTimer()
    {
        var factory = new XRayProcessTimerFactory();

        using var timer = factory.Create("test-segment");

        Assert.IsType<XRayProcessProcessTimer>(timer);
    }

    [Fact]
    public void Create_ImplementsIProcessTimerFactory()
    {
        IProcessTimerFactory factory = new XRayProcessTimerFactory();

        using var timer = factory.Create("test-segment");

        Assert.NotNull(timer);
    }
}
