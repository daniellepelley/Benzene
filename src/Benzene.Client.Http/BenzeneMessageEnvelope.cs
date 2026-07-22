using System.Collections.Generic;

namespace Benzene.Client.Http;

/// <summary>
/// The outbound BenzeneMessage envelope shape — <c>{ "topic": ..., "headers": { ... }, "body": "..." }</c> —
/// that the serving side's <c>BenzeneMessageRequest</c> deserializes (see
/// <c>docs/specification/wire-contracts.md</c> and <c>Benzene.Http</c>'s <c>BenzeneMessageHttpMiddleware</c>).
/// Defined locally so this HTTP package carries no dependency on <c>Benzene.Clients.Aws.Lambda</c>, where the
/// invoke-path <c>BenzeneMessageClientRequest</c> (the same wire shape) happens to live.
/// </summary>
internal sealed class BenzeneMessageEnvelope
{
    public string Topic { get; init; } = "";
    public IDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public string Body { get; init; } = "";
}
