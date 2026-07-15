using System.Linq;
using Benzene.Diagnostics.Timers;
using Benzene.Test.Logging.Helpers;
using Xunit;

namespace Benzene.Test.Diagnostics;

public class LoggingProcessTimerTest
{
    [Fact]
    public void Constructor_LogsAStartedMessage()
    {
        var collector = new FakeLogCollector();
        var logger = new FakeLogger(collector, "my-category");

        using (new LoggingProcessTimer("my-timer", logger))
        {
        }

        Assert.Contains(collector.Entries, e => e.Message == "my-timer started");
    }

    [Fact]
    public void Dispose_NoTagsSet_LogsElapsedTimeWithoutTags()
    {
        var collector = new FakeLogCollector();
        var logger = new FakeLogger(collector, "my-category");

        using (new LoggingProcessTimer("my-timer", logger))
        {
        }

        var completionMessage = collector.Entries.Last().Message;
        Assert.StartsWith("my-timer took", completionMessage);
        Assert.DoesNotContain("Tags", completionMessage);
    }

    [Fact]
    public void Dispose_TagsSet_LogsElapsedTimeWithEachTag()
    {
        var collector = new FakeLogCollector();
        var logger = new FakeLogger(collector, "my-category");

        using (var timer = new LoggingProcessTimer("my-timer", logger))
        {
            timer.SetTag("status", "ok");
        }

        var completionMessage = collector.Entries.Last().Message;
        Assert.StartsWith("my-timer took", completionMessage);
        Assert.Contains("Tags = status:ok", completionMessage);
    }

    [Fact]
    public void Create_DelegatesToLoggingProcessTimerWithTheGivenName()
    {
        var collector = new FakeLogCollector();
        var factory = new LoggingProcessTimerFactory(new FakeLogger<LoggingProcessTimer>(collector));

        using (factory.Create("my-timer"))
        {
        }

        Assert.Contains(collector.Entries, e => e.Message == "my-timer started");
    }
}
