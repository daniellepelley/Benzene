namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Scoped (one instance per message) record of the failure the handler layer converted into a
/// result — the thrown exception's TYPE and an optional remediation-hint key. The "scoped DI
/// state, not context" pattern (<see cref="PresetTopicHolder"/> is the canonical example): a
/// context type describes a transport message's shape and shouldn't accumulate cross-cutting
/// error metadata only some pipelines read.
/// </summary>
/// <remarks>
/// Written by <see cref="MessageHandler{TRequest,TResponse}"/>'s catch sites (the same three that
/// stamp <c>benzene.exception.type</c> via <see cref="ActivityExceptionTag"/> — this is the
/// Activity-free channel for readers that outlive the span). Read after <c>next()</c> by the mesh
/// feeds: <c>UseMeshTrace</c> populates <c>MeshTraceEvent.ExceptionType</c> from it, and
/// <c>UseMeshIssues</c> classifies with it (spec §4.1 — a thrown-and-converted failure classifies
/// by its throw, not its mapped status). First failure wins, matching the tag rule. Type name
/// only — never the message or stack trace (the stable, non-sensitive discriminator rule).
/// </remarks>
public class MessageErrorState
{
    /// <summary>The first caught exception's type name (e.g. <c>System.InvalidOperationException</c>),
    /// or null when nothing was thrown.</summary>
    public string? ExceptionTypeName { get; private set; }

    /// <summary>An optional spec-§4.1 remediation-hint key (e.g. <c>deserialization</c>), set by the
    /// catch site that knows the failure's mechanism; null otherwise.</summary>
    public string? Hint { get; private set; }

    /// <summary>Records the failure if none is recorded yet (first exception wins).</summary>
    public void TrySet(Exception exception, string? hint = null)
    {
        if (ExceptionTypeName != null)
        {
            return;
        }

        ExceptionTypeName = exception.GetType().FullName;
        Hint = hint;
    }
}
