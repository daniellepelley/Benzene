using System.Collections.Generic;
using Amazon.Lambda.SQSEvents;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Core.MessageHandlers;
using Xunit;

namespace Benzene.Examples.Aws.Tests.Framework.Routing;

public class SqsMessageMapperTests
{
    [Fact]
    public void SqsMessageMapperTest()
    {
        var sqsMessageContext = SqsMessageContext.CreateInstance(null, new SQSEvent.SQSMessage
        {
            Body = "some-message",
            MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>
            {
                { "topic", new SQSEvent.MessageAttribute { StringValue = "some-topic" } }
            }
        });

        var mapper = new MessageGetter<SqsMessageContext>(new SqsMessageTopicGetter(), new SqsMessageBodyGetter(), new SqsMessageHeadersGetter());

        var topic = mapper.GetTopic(sqsMessageContext);
        var message = mapper.GetBody(sqsMessageContext);

        Assert.Equal("some-topic", topic.Id);
        Assert.Equal("some-message", message);
    }

    [Fact]
    public void SqsMessageMapperTest_NoTopic()
    {
        var sqsMessageContext = SqsMessageContext.CreateInstance(null, new SQSEvent.SQSMessage
        {
            Body = "some-message",
            MessageAttributes = new Dictionary<string, SQSEvent.MessageAttribute>()
        });

        var mapper = new MessageGetter<SqsMessageContext>(new SqsMessageTopicGetter(), new SqsMessageBodyGetter(), new SqsMessageHeadersGetter());

        var topic = mapper.GetTopic(sqsMessageContext);
        var message = mapper.GetBody(sqsMessageContext);

        Assert.Equal(Constants.Missing.Id, topic.Id);
        Assert.Equal("some-message", message);
    }
}