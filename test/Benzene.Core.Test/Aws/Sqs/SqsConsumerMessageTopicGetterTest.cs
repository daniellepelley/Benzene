using System.Collections.Generic;
using Amazon.SQS.Model;
using Benzene.Aws.Sqs.Consumer;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Xunit;
using Constants = Benzene.Core.Constants;

namespace Benzene.Test.Aws.Sqs;

public class SqsConsumerMessageTopicGetterTest
{
    [Fact]
    public void GetTopic_NoTopicAttribute_ReturnsMissingTopicId()
    {
        var context = SqsConsumerMessageContext.CreateInstance(new Message
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
        });

        var topic = new SqsConsumerMessageTopicGetter().GetTopic(context);

        Assert.Equal(Constants.Missing, topic.Id);
    }

    [Fact]
    public void PresetTopicMessageTopicGetter_PresetSet_OverridesMissingTopicAttribute()
    {
        var context = SqsConsumerMessageContext.CreateInstance(new Message
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
        });
        var holder = new PresetTopicHolder { PresetTopic = new Topic("preset-topic") };

        var getter = new PresetTopicMessageTopicGetter<SqsConsumerMessageContext>(new SqsConsumerMessageTopicGetter(), holder);

        var topic = getter.GetTopic(context);

        Assert.Equal("preset-topic", topic.Id);
    }

    [Fact]
    public void GetTopic_ReadsCustomAttributeKey_WhenConfigured()
    {
        var context = SqsConsumerMessageContext.CreateInstance(new Message
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "x-my-topic", new MessageAttributeValue { StringValue = "some-topic", DataType = "String" } }
            }
        });

        var topic = new SqsConsumerMessageTopicGetter("x-my-topic").GetTopic(context);

        Assert.Equal("some-topic", topic.Id);
    }
}
