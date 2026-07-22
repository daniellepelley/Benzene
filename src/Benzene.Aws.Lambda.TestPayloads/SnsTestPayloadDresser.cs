using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.SNSEvents;
using Benzene.Schema.OpenApi.TestPayloads;

namespace Benzene.Aws.Lambda.TestPayloads;

/// <summary>
/// Dresses a topic's example payload as an <see cref="SNSEvent"/> - the shape an SNS-triggered Lambda
/// receives - ready to paste into the Lambda test console. Skips topics on a host not wired for SNS.
/// Mirrors the <c>sns</c> Lambda-test-tool dressing (<c>MessageBuilder.AsSns()</c>).
/// </summary>
public class SnsTestPayloadDresser : ITestPayloadDresser
{
    /// <inheritdoc />
    public string Transport => "sns";

    /// <inheritdoc />
    public object? Dress(TestPayloadDressingContext context)
    {
        if (!context.SupportsTransport(Transport))
        {
            return null;
        }

        var headers = new Dictionary<string, string>(context.Headers) { ["topic"] = context.Topic };

        var snsEvent = new SNSEvent
        {
            Records = new List<SNSEvent.SNSRecord>
            {
                new SNSEvent.SNSRecord
                {
                    EventSource = "aws:sns",
                    Sns = new SNSEvent.SNSMessage
                    {
                        MessageAttributes = headers.ToDictionary(
                            x => x.Key,
                            x => new SNSEvent.MessageAttribute { Value = x.Value, Type = "String" }),
                        Message = context.SerializedBody,
                    },
                },
            },
        };

        return AwsEventJson.ToToken(snsEvent);
    }
}
