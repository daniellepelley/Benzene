using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Benzene.Aws.Lambda.TestPayloads;

/// <summary>
/// Serializes an AWS Lambda event POCO to a <see cref="JToken"/> with its own canonical property
/// casing. Returning a <see cref="JToken"/> (rather than the POCO) means the test-payloads manifest's
/// camelCase serializer embeds it verbatim, so the SNS/SQS/API-Gateway envelopes keep the exact shape
/// the Lambda test console expects instead of being re-cased to camelCase.
/// </summary>
internal static class AwsEventJson
{
    public static JToken ToToken(object awsEvent) => JToken.Parse(JsonConvert.SerializeObject(awsEvent));
}
