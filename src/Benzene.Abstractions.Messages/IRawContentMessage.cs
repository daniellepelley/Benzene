namespace Benzene.Abstractions.Messages;

/// <summary>
/// A raw payload that also carries its own content type, so a response renderer can set the
/// transport's content type from the payload itself instead of the negotiated format - e.g. a
/// handler returning pre-rendered HTML alongside <c>"text/html"</c>, delivered as-is regardless of
/// what format negotiation would otherwise have selected.
/// </summary>
public interface IRawContentMessage : IRawStringMessage
{
    /// <summary>The content type this payload should be delivered with.</summary>
    string ContentType { get; }
}
