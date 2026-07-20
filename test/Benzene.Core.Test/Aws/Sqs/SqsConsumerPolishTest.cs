using System.Collections.Generic;
using Amazon.SQS.Model;
using Benzene.Aws.Sqs.Consumer;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

/// <summary>
/// Covers the SQS consumer polish (#30.2): long-polling default and the ApproximateReceiveCount
/// surfaced on the message context for poison-message decisions.
/// </summary>
public class SqsConsumerPolishTest
{
    [Fact]
    public void SqsConsumerConfig_WaitTimeSeconds_DefaultsTo20()
    {
        Assert.Equal(20, new SqsConsumerConfig().WaitTimeSeconds);
    }

    [Fact]
    public void ApproximateReceiveCount_IsParsedFromTheSystemAttribute()
    {
        var context = SqsConsumerMessageContext.CreateInstance(new Message
        {
            Attributes = new Dictionary<string, string> { { "ApproximateReceiveCount", "3" } }
        });

        Assert.Equal(3, context.ApproximateReceiveCount);
    }

    [Fact]
    public void ApproximateReceiveCount_IsNull_WhenAbsent()
    {
        var context = SqsConsumerMessageContext.CreateInstance(new Message());

        Assert.Null(context.ApproximateReceiveCount);
    }
}
