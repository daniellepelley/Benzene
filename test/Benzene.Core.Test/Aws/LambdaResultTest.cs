﻿using Benzene.Core.MessageHandlers;
using Benzene.Core.Results;
using Xunit;

namespace Benzene.Test.Aws;

public class LambdaResultTest
{
    [Fact]
    public void LambdaResultPopulates()
    {
        const string topic = "some-topic";
        const string status = "some-status";
        const string error = "some-error";

        var lambdaResult = new MessageResult(topic, null, status, false, new object(), new[] { error });
            
        Assert.Equal(topic, lambdaResult.Topic);
        Assert.Equal(status, lambdaResult.Status);
        Assert.Equal(error, lambdaResult.Errors[0]);
    }
}