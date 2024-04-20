using System.Collections.Generic;
using Amazon.Lambda.SNSEvents;
using Benzene.Aws.Core.Sns;
using Benzene.Core.Mappers;
using Xunit;

namespace Benzene.Examples.Aws.Tests.Framework.Routing;

public class SnsMessageMapperTests
{
    [Fact]
    public void SnsMessageMapperTest()
    {
        var snsRecordContext = new SnsRecordContext(null, new SNSEvent.SNSRecord
        {
            Sns = new SNSEvent.SNSMessage
            {
                Message = "some-message",
                MessageAttributes = new Dictionary<string, SNSEvent.MessageAttribute>
                {
                    {"topic", new SNSEvent.MessageAttribute { Value = "some-topic"}}
                }
            }
        });
        
        var mapper = new MessageMapper<SnsRecordContext>(new SnsMessageTopicMapper(), new SnsMessageBodyMapper(), new SnsMessageHeadersMapper());

        var topic = mapper.GetTopic(snsRecordContext);
        var message = mapper.GetBody(snsRecordContext);

        Assert.Equal("some-topic", topic.Id);
        Assert.Equal("some-message", message);
    }
}