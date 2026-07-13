using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Client.Http;
using Benzene.Clients;
using Benzene.Clients.Aws.Sns;
using Benzene.Clients.Aws.Sqs;
using Benzene.Kafka.Core.Kafka;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Clients;

public class OutboundHeaderForwardingTest
{
    private const string HeaderKey = "traceparent";
    private const string HeaderValue = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

    private static BenzeneClientContext<string, Void> CreateContext() =>
        new(new BenzeneClientRequest<string>("my-topic", "hello", new Dictionary<string, string> { { HeaderKey, HeaderValue } }));

    [Fact]
    public async Task HttpContextConverter_ForwardsHeadersOntoTheRequest()
    {
        var converter = new HttpContextConverter<string, Void>("POST", "https://example.test/endpoint");
        var context = await converter.CreateRequestAsync(CreateContext());

        Assert.Equal(HeaderValue, context.Request.Headers.GetValues(HeaderKey).Single());
    }

    [Fact]
    public async Task SqsContextConverter_ForwardsHeaders_AlongsideTopicAttribute()
    {
        var converter = new SqsContextConverter<string>("https://example.test/queue");
        var context = await converter.CreateRequestAsync(CreateContext());

        Assert.Equal(HeaderValue, context.Request.MessageAttributes[HeaderKey].StringValue);
        Assert.Equal("my-topic", context.Request.MessageAttributes["topic"].StringValue);
    }

    [Fact]
    public async Task SnsContextConverter_ForwardsHeaders()
    {
        var converter = new SnsContextConverter<string>("arn:aws:sns:us-east-1:000000000000:example-topic");
        var context = await converter.CreateRequestAsync(CreateContext());

        Assert.Equal(HeaderValue, context.Request.MessageAttributes[HeaderKey].StringValue);
    }

    [Fact]
    public async Task KafkaContextConverter_ForwardsHeaders()
    {
        var converter = new KafkaContextConverter<string>();
        var context = await converter.CreateRequestAsync(CreateContext());

        var header = context.Message.Headers.Single(h => h.Key == HeaderKey);
        Assert.Equal(HeaderValue, Encoding.UTF8.GetString(header.GetValueBytes()));
    }
}
