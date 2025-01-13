using System.Collections.Generic;
using Amazon.Lambda.SNSEvents;
using Benzene.Aws.Sns;
using Benzene.Core;
using Benzene.Core.Mappers;
using Xunit;

namespace Benzene.Test.Aws.Sns
{
    public class SnsMessageMapperTests
    {
        [Fact]
        public void SnsMessageMapperTest()
        {
            var snsRecordContext = SnsRecordContext.CreateInstance(null, new SNSEvent.SNSRecord
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

            var mapper = new MessageGetter<SnsRecordContext>(new SnsMessageTopicGetter(), new SnsMessageBodyGetter(), new SnsMessageHeadersGetter());

            var topic = mapper.GetTopic(snsRecordContext);
            var message = mapper.GetBody(snsRecordContext);

            Assert.Equal("some-topic", topic.Id);
            Assert.Equal("some-message", message);
        }
        
        [Fact]
        public void SnsMessageMapperTest_NoTopic()
        {
            var snsRecordContext = SnsRecordContext.CreateInstance(null, new SNSEvent.SNSRecord
            {
                Sns = new SNSEvent.SNSMessage
                {
                    Message = "some-message",
                    MessageAttributes = new Dictionary<string, SNSEvent.MessageAttribute>()
                }
            });

            var mapper = new MessageGetter<SnsRecordContext>(new SnsMessageTopicGetter(), new SnsMessageBodyGetter(), new SnsMessageHeadersGetter());

            var topic = mapper.GetTopic(snsRecordContext);
            var message = mapper.GetBody(snsRecordContext);

            Assert.Equal(Constants.Missing, topic.Id);
            Assert.Equal("some-message", message);
        }
    }
}
