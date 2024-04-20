using Xunit;

namespace Benzene.Examples.Kafka.Test.Helpers;

public static class ResultPoller
{
    public static async Task Poll(int delay, int times, Func<bool> check, string failureMessage)
    {
        for (var i = 0; i < times; i++)
        {
            await Task.Delay(delay);
            if (check())
            {
                return;
            }
        }

        Assert.Fail(failureMessage);
    }
}