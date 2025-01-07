using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Abstractions.Logging;
using Benzene.Clients;
using Benzene.Clients.Aws.Sqs;
using Benzene.Core.Logging;
using Benzene.Core.Middleware;
using Benzene.Results;
using Benzene.Test.Examples;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Test.Clients.Aws.Sqs;

public class SqsBenzeneMessageClientTest
{
    [Fact]
    public async Task FireAndForget()
    {
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        mockAmazonSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            });

        var client = new SqsBenzeneMessageClientFactory(Defaults.SqsQueueUrl, mockAmazonSqs.Object, Mock.Of<IBenzeneLogger>()).Create();
        var result = await client.SendMessageAsync<ExampleRequestPayload, ExampleResponsePayload>(Defaults.Topic, 
                new ExampleRequestPayload { Id = 42, Name = "foo" });

        mockAmazonSqs.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(message =>
                message.QueueUrl == Defaults.SqsQueueUrl &&
                message.MessageAttributes["topic"].StringValue == Defaults.Topic &&
                JsonConvert.DeserializeObject<ExampleRequestPayload>(message.MessageBody).Name == "foo"
                ), It.IsAny<CancellationToken>()));

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
    
    [Fact]
    public async Task FireAndForget_Exception()
    {
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        mockAmazonSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception());

        var client = new SqsBenzeneMessageClientFactory(Defaults.SqsQueueUrl, mockAmazonSqs.Object, Mock.Of<IBenzeneLogger>()).Create();
        var result = await client.SendMessageAsync<ExampleRequestPayload, ExampleResponsePayload>(Defaults.Topic, 
                new ExampleRequestPayload { Id = 42, Name = "hi" });

        mockAmazonSqs.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(message =>
                message.QueueUrl == Defaults.SqsQueueUrl &&
                message.MessageAttributes["topic"].StringValue == Defaults.Topic &&
                JsonConvert.DeserializeObject<ExampleRequestPayload>(message.MessageBody).Name == "hi"
                ), It.IsAny<CancellationToken>()));

        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, result.Status);
    }

    [Fact]
    public async Task ClientMessageSender()
    {
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        mockAmazonSqs.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            });

        var mockClientMessageRouter = new Mock<IClientMessageRouter>();
        mockClientMessageRouter.Setup(x => x.GetClient<ExampleRequestPayload>())
            .Returns(new SqsBenzeneMessageClient(Defaults.SqsQueueUrl, mockAmazonSqs.Object, new BenzeneLogger(new List<IBenzeneLogAppender>()), new NullServiceResolver()));

        var getTopic = new DictionaryGetTopic(new Dictionary<Type, string>
        {
            { typeof(ExampleRequestPayload), Defaults.Topic }
        });
        
        var sender = new ClientMessageSender<ExampleRequestPayload, ExampleResponsePayload>(mockClientMessageRouter.Object, getTopic);
        
        var result = await sender.SendMessageAsync(new ExampleRequestPayload { Id = 42, Name = "foo" });
       
        mockAmazonSqs.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(message =>
                message.QueueUrl == Defaults.SqsQueueUrl &&
                message.MessageAttributes["topic"].StringValue == Defaults.Topic &&
                JsonConvert.DeserializeObject<ExampleRequestPayload>(message.MessageBody).Name == "foo"
                ), It.IsAny<CancellationToken>()));

        Assert.Equal(BenzeneResultStatus.Accepted, result.Status);
    }
}

public class GetTopicTest
{
    [Fact]
    public void DictionaryGetTopic()
    {
        var dictionaryGetTopic = new DictionaryGetTopic(new Dictionary<Type, string>
        {
            { typeof(ExampleRequestPayload), Defaults.Topic }
        });

        var topic = dictionaryGetTopic.GetTopic(typeof(ExampleRequestPayload));

        Assert.Equal(Defaults.Topic, topic);
    }
}

public class DictionaryGetTopic : IGetTopic
{
    private readonly IDictionary<Type, string> _dictionary;

    public DictionaryGetTopic(IDictionary<Type, string> dictionary)
    {
        _dictionary = dictionary;
    }

    public string GetTopic(Type type)
    {
        return _dictionary[type];
    }
}
