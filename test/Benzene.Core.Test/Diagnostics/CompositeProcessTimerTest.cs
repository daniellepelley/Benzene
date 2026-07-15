using Benzene.Diagnostics.Timers;
using Moq;
using Xunit;

namespace Benzene.Test.Diagnostics;

public class CompositeProcessTimerTest
{
    [Fact]
    public void SetTag_FansOutToEveryInnerTimer()
    {
        var first = new Mock<IProcessTimer>();
        var second = new Mock<IProcessTimer>();
        var composite = new CompositeProcessTimer(new[] { first.Object, second.Object });

        composite.SetTag("key", "value");

        first.Verify(x => x.SetTag("key", "value"), Times.Once);
        second.Verify(x => x.SetTag("key", "value"), Times.Once);
    }

    [Fact]
    public void Dispose_FansOutToEveryInnerTimer()
    {
        var first = new Mock<IProcessTimer>();
        var second = new Mock<IProcessTimer>();
        var composite = new CompositeProcessTimer(new[] { first.Object, second.Object });

        composite.Dispose();

        first.Verify(x => x.Dispose(), Times.Once);
        second.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Create_FansOutToEveryInnerFactory_UsingTheSameTimerName()
    {
        var firstTimer = new Mock<IProcessTimer>();
        var firstFactory = new Mock<IProcessTimerFactory>();
        firstFactory.Setup(x => x.Create("my-timer")).Returns(firstTimer.Object);

        var secondTimer = new Mock<IProcessTimer>();
        var secondFactory = new Mock<IProcessTimerFactory>();
        secondFactory.Setup(x => x.Create("my-timer")).Returns(secondTimer.Object);

        var compositeFactory = new CompositeProcessTimerFactory(firstFactory.Object, secondFactory.Object);

        using (compositeFactory.Create("my-timer"))
        {
        }

        firstFactory.Verify(x => x.Create("my-timer"), Times.Once);
        secondFactory.Verify(x => x.Create("my-timer"), Times.Once);
        firstTimer.Verify(x => x.Dispose(), Times.Once);
        secondTimer.Verify(x => x.Dispose(), Times.Once);
    }
}
