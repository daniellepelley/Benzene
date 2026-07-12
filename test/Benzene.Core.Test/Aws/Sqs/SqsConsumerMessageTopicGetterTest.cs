using System.Collections.Generic;
using Amazon.SQS.Model;
using Benzene.Aws.Sqs.Consumer;
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
}
