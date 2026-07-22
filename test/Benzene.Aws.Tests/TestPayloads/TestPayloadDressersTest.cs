using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using Benzene.Aws.Lambda.TestPayloads;
using Benzene.Schema.OpenApi.TestPayloads;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Benzene.Aws.Tests.TestPayloads;

public class TestPayloadDressersTest
{
    private const string Topic = "order:placed";
    private const string Body = "{\"orderId\":\"abc\"}";

    private static TestPayloadDressingContext Context(string[] transports, params (string Method, string Path)[] httpMappings)
    {
        return new TestPayloadDressingContext(
            Topic,
            new Dictionary<string, string>(),
            Body,
            transports,
            httpMappings.Select(m => new TestPayloadHttpMapping { Method = m.Method, Path = m.Path }).ToArray());
    }

    [Fact]
    public void Sns_WhenWired_DressesTopicAsSnsEvent()
    {
        var dressed = new SnsTestPayloadDresser().Dress(Context(new[] { "sns" }));

        var snsEvent = Assert.IsAssignableFrom<JToken>(dressed).ToObject<SNSEvent>()!;
        var record = Assert.Single(snsEvent.Records);
        Assert.Equal("aws:sns", record.EventSource);
        Assert.Equal(Body, record.Sns.Message);
        Assert.Equal(Topic, record.Sns.MessageAttributes["topic"].Value);
    }

    [Fact]
    public void Sns_WhenNotWired_ReturnsNull()
    {
        Assert.Null(new SnsTestPayloadDresser().Dress(Context(new[] { "http" })));
    }

    [Fact]
    public void Sqs_WhenWired_DressesTopicAsSqsEvent()
    {
        var dressed = new SqsTestPayloadDresser().Dress(Context(new[] { "sqs" }));

        var sqsEvent = Assert.IsAssignableFrom<JToken>(dressed).ToObject<SQSEvent>()!;
        var record = Assert.Single(sqsEvent.Records);
        Assert.Equal("aws:sqs", record.EventSource);
        Assert.Equal(Body, record.Body);
        Assert.Equal(Topic, record.MessageAttributes["topic"].StringValue);
        // Deterministic placeholder id (no random Guid) so the manifest is stable per build.
        Assert.Equal("00000000-0000-0000-0000-000000000000", record.MessageId);
    }

    [Fact]
    public void Sqs_WhenNotWired_ReturnsNull()
    {
        Assert.Null(new SqsTestPayloadDresser().Dress(Context(new[] { "sns" })));
    }

    [Fact]
    public void ApiGateway_WhenMapped_DressesTopicAsProxyRequest()
    {
        var dressed = new ApiGatewayTestPayloadDresser().Dress(Context(new[] { "http" }, ("POST", "/orders")));

        var request = Assert.IsAssignableFrom<JToken>(dressed).ToObject<APIGatewayProxyRequest>()!;
        Assert.Equal("POST", request.HttpMethod);
        Assert.Equal("/orders", request.Path);
        Assert.Equal(Body, request.Body);
    }

    [Fact]
    public void ApiGateway_WhenNoMappings_ReturnsNull()
    {
        Assert.Null(new ApiGatewayTestPayloadDresser().Dress(Context(new[] { "http" })));
    }
}
