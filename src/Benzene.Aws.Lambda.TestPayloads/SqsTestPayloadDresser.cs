using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.SQSEvents;
using Benzene.Schema.OpenApi.TestPayloads;

namespace Benzene.Aws.Lambda.TestPayloads;

/// <summary>
/// Dresses a topic's example payload as an <see cref="SQSEvent"/> - the shape an SQS-triggered Lambda
/// receives - ready to paste into the Lambda test console. Skips topics on a host not wired for SQS.
/// Mirrors the <c>sqs</c> Lambda-test-tool dressing (<c>MessageBuilder.AsSqs()</c>).
/// </summary>
public class SqsTestPayloadDresser : ITestPayloadDresser
{
    // A fixed, obviously-placeholder id keeps the manifest deterministic - the runtime test-payloads
    // endpoint is polled/cacheable like the spec, so identical output per build matters (the live AsSqs
    // helper uses a random Guid, which is right for a one-shot test but wrong for a stable manifest).
    private const string PlaceholderMessageId = "00000000-0000-0000-0000-000000000000";

    /// <inheritdoc />
    public string Transport => "sqs";

    /// <inheritdoc />
    public object? Dress(TestPayloadDressingContext context)
    {
        if (!context.SupportsTransport(Transport))
        {
            return null;
        }

        var headers = new Dictionary<string, string>(context.Headers) { ["topic"] = context.Topic };

        var sqsEvent = new SQSEvent
        {
            Records = new List<SQSEvent.SQSMessage>
            {
                new SQSEvent.SQSMessage
                {
                    MessageId = PlaceholderMessageId,
                    EventSource = "aws:sqs",
                    MessageAttributes = headers.ToDictionary(
                        x => x.Key,
                        x => new SQSEvent.MessageAttribute { StringValue = x.Value, DataType = "String" }),
                    Body = context.SerializedBody,
                },
            },
        };

        return AwsEventJson.ToToken(sqsEvent);
    }
}
