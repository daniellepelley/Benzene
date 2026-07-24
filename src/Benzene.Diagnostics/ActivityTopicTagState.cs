namespace Benzene.Diagnostics;

/// <summary>
/// Per-message (scoped) flag marking whether the message-identity tags
/// (<c>benzene.topic</c>/<c>benzene.version</c>/<c>benzene.handler</c>/<c>benzene.correlation-id</c>, and the
/// after-<c>next()</c> <c>benzene.status</c>) have already been stamped on a span in this dispatch. Because
/// every middleware stage runs in the same per-message DI scope, resolving this scoped holder from the same
/// resolver <see cref="ActivityMiddlewareDecorator{TContext}"/> uses for the topic getter yields one shared
/// instance across the whole pipeline - so the FIRST topic-bearing span sets <see cref="Tagged"/> and every
/// later stage skips those tags. Without it, transports whose topic is intrinsic to the message (HTTP route,
/// BenzeneMessage envelope) resolve a topic at every stage and stamp the identity tags on every span, which a
/// trace-backed mesh reader (<c>Benzene.Mesh.Fleet.*</c>) then counts as one flow event per stage.
/// Registered scoped by <see cref="DependencyInjectionExtensions.AddActivityPerMiddleware"/>.
/// </summary>
public class ActivityTopicTagState
{
    /// <summary>Whether the identity tags have already been stamped on a span in this dispatch.</summary>
    public bool Tagged { get; set; }
}
