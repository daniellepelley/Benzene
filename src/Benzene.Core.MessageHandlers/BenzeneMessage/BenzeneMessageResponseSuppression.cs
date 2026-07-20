namespace Benzene.Core.MessageHandlers.BenzeneMessage;

/// <summary>
/// Scoped, per-message flag telling <see cref="BenzeneMessageHandlerResultSetter"/> to skip writing
/// (serializing) the response for this message. Set by <see cref="SuppressBenzeneMessageResponseMiddleware"/>
/// (added via <c>SuppressResponse()</c>) on a BenzeneMessage pipeline hosted by a one-way transport -
/// Event Hub, Queue Storage, and the like - which discards the response anyway, so serializing it is
/// wasted work on a hot path.
/// </summary>
/// <remarks>
/// A fresh instance exists per message (registered scoped; a new DI scope is created per message),
/// so it defaults to not-suppressed and only the specific pipeline that added the middleware is
/// affected - request/response BenzeneMessage usage (direct Lambda invoke, HTTP, tests) is untouched.
/// This is the scoped-DI-holder pattern (see <c>Benzene.Abstractions.Middleware/CLAUDE.md</c>'s
/// "Context purity" section and <see cref="PresetTopicHolder"/>), not a property on
/// <c>BenzeneMessageContext</c>.
/// </remarks>
public class BenzeneMessageResponseSuppression
{
    /// <summary>Whether the response for the current message should not be written.</summary>
    public bool IsSuppressed { get; set; }
}
