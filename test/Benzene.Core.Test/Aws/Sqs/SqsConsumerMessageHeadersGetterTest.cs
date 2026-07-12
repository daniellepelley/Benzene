using System.Collections.Generic;
using Amazon.SQS.Model;
using Benzene.Aws.Sqs.Consumer;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

public class SqsConsumerMessageHeadersGetterTest
{
    [Fact]
    public void GetHeaders_ReturnsOnlyStringTypedAttributes()
    {
        var context = SqsConsumerMessageContext.CreateInstance(new Message
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { "topic", new MessageAttributeValue { DataType = "String", StringValue = "some-topic" } },
                { "count", new MessageAttributeValue { DataType = "Number", StringValue = "5" } }
            }
        });

        var headers = new SqsConsumerMessageHeadersGetter().GetHeaders(context);

        Assert.Equal("some-topic", headers["topic"]);
        Assert.False(headers.ContainsKey("count"));
    }
}
