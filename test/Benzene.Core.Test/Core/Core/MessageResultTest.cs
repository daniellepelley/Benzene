using Benzene.Core.MessageHandlers;
using Xunit;

namespace Benzene.Test.Core.Core;

public class MessageResultTest
{
    [Fact]
    public void LambdaResultPopulates()
    {
        const string topic = "some-topic";
        const string status = "some-status";
        const string error = "some-error";

        var messageResult = new MessageResult(new Topic(topic), null, status, true, new object(), new[] { error });

        Assert.Equal(topic, messageResult.Topic.Id);
        Assert.Equal(status, messageResult.Status);
        Assert.Equal(error, messageResult.Errors[0]);
    }
}
