using Benzene.Abstractions.Messages;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Scoped (one instance per message) cache of the message's resolved <see cref="ITopic"/>. The topic
/// is extracted from the transport context once and reused, instead of being re-extracted
/// independently by every consumer that needs it - the router, the health-check middleware, and each
/// observability decorator's per-subsegment tagging all call
/// <see cref="Benzene.Abstractions.MessageHandlers.Mappers.IMessageGetter{TContext}.GetTopic"/>. On a
/// traced Lambda that is ~a dozen extractions per message collapsed to one.
/// </summary>
/// <remarks>
/// Follows the same scoped-holder pattern as <see cref="PresetTopicHolder"/> and the memoizing
/// <c>MediaFormatNegotiator</c> (one selection per message). It is <b>generic on
/// <typeparamref name="TContext"/></b> so a multi-transport function (one invocation resolving, say,
/// <c>IMessageGetter&lt;SqsContext&gt;</c> and then <c>IMessageGetter&lt;SnsContext&gt;</c>) keeps a
/// separate cache per context type and never serves one transport's topic to another.
/// </remarks>
/// <typeparam name="TContext">The transport context type the topic is extracted from.</typeparam>
public class ResolvedTopicCache<TContext>
{
    private ITopic? _topic;

    /// <summary>Whether a topic has been resolved and cached for the current message.</summary>
    public bool HasValue { get; private set; }

    /// <summary>The cached topic (only meaningful when <see cref="HasValue"/> is <c>true</c>).</summary>
    public ITopic? Topic => _topic;

    /// <summary>Records the resolved topic for the current message.</summary>
    public void Set(ITopic? topic)
    {
        _topic = topic;
        HasValue = true;
    }

    /// <summary>
    /// Discards any cached topic. Called when a preset topic is applied mid-pipeline
    /// (<see cref="PresetTopicMiddleware{TContext}"/>) so a topic memoized <i>before</i> the preset was
    /// set is not served to the router afterwards - the next extraction re-resolves with the preset in
    /// effect.
    /// </summary>
    public void Reset()
    {
        _topic = null;
        HasValue = false;
    }
}
