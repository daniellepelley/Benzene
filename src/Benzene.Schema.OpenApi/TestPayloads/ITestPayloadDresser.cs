namespace Benzene.Schema.OpenApi.TestPayloads;

/// <summary>
/// Dresses a topic's example payload for one transport - e.g. wrapping it in an SNS/SQS event or an
/// API Gateway request. Implementations live in transport-specific opt-in packages: the runtime-safe
/// core (<see cref="TestPayloadsBuilder"/>) carries only the portable <c>benzene-message</c> envelope
/// and has no AWS/transport coupling. A host that wants transport-dressed payloads registers the
/// relevant dressers in DI; the builder then folds each dresser's output into every topic's
/// <see cref="TestPayloadTopic.Payloads"/> under the dresser's <see cref="Transport"/> key.
/// </summary>
public interface ITestPayloadDresser
{
    /// <summary>The transport key this dresser produces (e.g. <c>sns</c>, <c>sqs</c>, <c>api-gateway</c>).</summary>
    string Transport { get; }

    /// <summary>
    /// Produces the transport-dressed payload for a topic, or <c>null</c> to skip it - e.g. an
    /// API Gateway dresser skips a topic with no HTTP mappings, and an SNS dresser skips a host that
    /// isn't wired for SNS. The returned object is serialized verbatim under the transport's manifest
    /// key (return a pre-parsed <c>JToken</c> to keep a transport's own canonical property casing,
    /// which the manifest's camelCase serializer leaves untouched).
    /// </summary>
    object? Dress(TestPayloadDressingContext context);
}
