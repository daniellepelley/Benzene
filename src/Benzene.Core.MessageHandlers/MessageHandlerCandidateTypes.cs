namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Records the candidate types handed to one reflection-scanning <c>AddMessageHandlers</c> call, so
/// cross-cutting startup diagnostics can inspect the same set of types handler discovery saw —
/// including the types discovery <em>skipped</em> (e.g. a handler missing its
/// <see cref="MessageAttribute"/>), which the discovered definitions alone can't reveal.
/// Registered cumulatively (one instance per call, resolved via <c>GetServices</c>); consumed by
/// <c>Benzene.Http</c>'s unrouted-endpoint check.
/// </summary>
public class MessageHandlerCandidateTypes
{
    /// <summary>Initializes the record over one scan's candidate types.</summary>
    /// <param name="types">The candidate types passed to <c>AddMessageHandlers</c>.</param>
    public MessageHandlerCandidateTypes(Type[] types)
    {
        Types = types;
    }

    /// <summary>Gets the candidate types this scan inspected.</summary>
    public IReadOnlyList<Type> Types { get; }
}
