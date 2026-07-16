using Benzene.Abstractions.Messages;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Carries a preset <see cref="ITopic"/> for the current message, set by
/// <see cref="PresetTopicMiddleware{TContext}"/> (via the <c>UsePresetTopic</c> pipeline extension)
/// and read back by <see cref="PresetTopicMessageTopicGetter{TContext}"/>.
/// </summary>
/// <remarks>
/// Registered scoped (one instance per message, alongside the rest of that message's DI scope) -
/// deliberately NOT carried on the transport context. A context type describes the shape of a
/// transport message; it should stay free of optional, cross-cutting routing overrides that only
/// some pipelines opt into. Scoped DI state is the seam for that instead: a queue/subscription that
/// never calls <c>UsePresetTopic</c> never constructs a <see cref="PresetTopicMiddleware{TContext}"/>,
/// so this holder's <see cref="PresetTopic"/> simply stays <c>null</c> for that message, and
/// <see cref="PresetTopicMessageTopicGetter{TContext}"/> falls through to the transport's real
/// getter - unchanged behavior, no context coupling. See
/// <c>Benzene.Abstractions.Middleware/CLAUDE.md</c> for the general pattern this follows.
/// </remarks>
public class PresetTopicHolder
{
    /// <summary>Gets or sets the preset topic for the current message, or <c>null</c> if none has been set.</summary>
    public ITopic? PresetTopic { get; set; }
}
