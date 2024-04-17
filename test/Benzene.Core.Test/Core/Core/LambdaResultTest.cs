using Benzene.Core.Results;
using Xunit;

namespace Benzene.Test.Core.Core;

public class LambdaResultTest
{
    [Fact]
    public void LambdaResultPopulates()
    {
        const string topic = "some-topic";
        const string status = "some-status";
        const string error = "some-error";

        var messageResult = new MessageResult(topic, null, status, true, new object(), new[] { error });

        Assert.Equal(topic, messageResult.Topic);
        Assert.Equal(status, messageResult.Status);
        Assert.Equal(error, messageResult.Errors[0]);
    }
}
