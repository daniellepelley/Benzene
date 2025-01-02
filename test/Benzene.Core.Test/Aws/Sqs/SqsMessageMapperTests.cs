using System.Collections.Generic;
using Amazon.Lambda.SQSEvents;
using Benzene.Aws.Lambda.Sqs;
using Benzene.Aws.Sqs;
using Benzene.Core;
using Benzene.Core.Mappers;
using Xunit;

namespace Benzene.Test.Aws.Sqs
{
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

            var mapper = new MessageMapper<SqsMessageContext>(new SqsMessageTopicMapper(), new SqsMessageBodyMapper(), new SqsMessageHeadersMapper());

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

            var mapper = new MessageMapper<SqsMessageContext>(new SqsMessageTopicMapper(), new SqsMessageBodyMapper(), new SqsMessageHeadersMapper());

            var topic = mapper.GetTopic(sqsMessageContext);
            var message = mapper.GetBody(sqsMessageContext);

            Assert.Equal(Constants.Missing, topic.Id);
            Assert.Equal("some-message", message);
        }
    }
}
