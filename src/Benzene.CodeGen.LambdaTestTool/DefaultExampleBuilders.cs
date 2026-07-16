using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Aws.Lambda.Sns.TestHelpers;
using Benzene.Aws.Lambda.Sqs.TestHelpers;
using Benzene.CodeGen.Core;
using Benzene.Testing;
using Newtonsoft.Json;

namespace Benzene.CodeGen.LambdaTestTool;

/// <summary>
/// The standard set of per-transport example builders used to generate Lambda Test Tool
/// saved-request files: the BenzeneMessage envelope (direct invoke / the
/// <c>/benzene-message</c> HTTP endpoint), SNS, SQS, and API Gateway (only emitted for topics
/// with HTTP mappings). Payloads come from the spec-driven example generator
/// (<c>Benzene.Schema.OpenApi.Examples.ExamplePayloadBuilder</c>), so the same deterministic,
/// validation-aware examples appear here as in the spec itself.
/// </summary>
public static class DefaultExampleBuilders
{
    /// <summary>
    /// Creates the standard example builder set with no known-value overrides.
    /// </summary>
    public static IExampleBuilder[] Create()
    {
        return Create(new Dictionary<string, object>());
    }

    /// <summary>
    /// Creates the standard example builder set.
    /// </summary>
    /// <param name="knownValues">
    /// Values that override generation, keyed by camelCased property path or bare property name
    /// (see <c>ExamplePayloadBuilder</c>).
    /// </param>
    public static IExampleBuilder[] Create(IDictionary<string, object> knownValues)
    {
        return new IExampleBuilder[]
        {
            new ExampleBuilder("benzene-message", (topic, payload) => new
            {
                topic,
                headers = new Dictionary<string, string>(),
                body = JsonConvert.SerializeObject(payload)
            }, knownValues),
            new ExampleBuilder("sns", (topic, payload) => MessageBuilder.Create(topic, payload).AsSns(), knownValues),
            new ExampleBuilder("sqs", (topic, payload) => MessageBuilder.Create(topic, payload).AsSqs(), knownValues),
            new HttpExampleBuilder("api-gateway", (method, path, payload) => HttpBuilder.Create(method, path, payload).AsApiGatewayRequest(), knownValues)
        };
    }
}
