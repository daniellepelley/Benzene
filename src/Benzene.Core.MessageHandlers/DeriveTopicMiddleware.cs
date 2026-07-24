using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Derives the routing <see cref="ITopic"/> for the current message by running a caller-supplied
/// selector over the transport context, then sets it on the current message's
/// <see cref="PresetTopicHolder"/> so <see cref="PresetTopicMessageTopicGetter{TContext}"/> resolves
/// it in preference to whatever the transport itself would extract. The dynamic sibling of
/// <see cref="PresetTopicMiddleware{TContext}"/>: <c>UsePresetTopic</c> sets a fixed topic, this
/// (<c>UseTopicFrom</c>) computes one per message from the payload. Added before
/// <c>UseMessageHandlers</c> for one specific queue/subscription's pipeline.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the selector reads the payload off.</typeparam>
public class DeriveTopicMiddleware<TContext> : IMiddleware<TContext>
{
    private readonly PresetTopicHolder _holder;
    private readonly Func<TContext, ITopic?> _topicSelector;
    private readonly ResolvedTopicCache<TContext>? _resolvedTopicCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeriveTopicMiddleware{TContext}"/> class.
    /// </summary>
    /// <param name="holder">The current message's preset-topic holder, resolved from the same DI scope <see cref="PresetTopicMessageTopicGetter{TContext}"/> reads it from.</param>
    /// <param name="topicSelector">Computes the topic for a message from its context; return <c>null</c> to fall through to the transport's own topic extraction for that message.</param>
    /// <param name="resolvedTopicCache">
    /// The scoped per-message topic cache to discard when a topic is derived, so a topic memoized by an
    /// earlier middleware (e.g. a tracing decorator) isn't served to the router in place of the derived
    /// one. Optional (<c>null</c> in a direct construction) - DI supplies it.
    /// </param>
    public DeriveTopicMiddleware(PresetTopicHolder holder, Func<TContext, ITopic?> topicSelector, ResolvedTopicCache<TContext>? resolvedTopicCache = null)
    {
        _holder = holder;
        _topicSelector = topicSelector;
        _resolvedTopicCache = resolvedTopicCache;
    }

    /// <inheritdoc />
    public string Name => "DeriveTopic";

    /// <inheritdoc />
    public Task HandleAsync(TContext context, Func<Task> next)
    {
        var topic = _topicSelector(context);

        // Only override when the selector produced a real topic - a null/empty result means "I can't
        // derive one from this message", so leave the holder unset and let the transport's own getter
        // (the inner getter PresetTopicMessageTopicGetter falls through to) have its say.
        if (topic is not null && !string.IsNullOrEmpty(topic.Id))
        {
            _holder.PresetTopic = topic;
            // Anything that resolved the topic before this middleware ran cached the transport's own
            // (likely null) topic; drop it so the router re-resolves and sees the derived one.
            _resolvedTopicCache?.Reset();
        }

        return next();
    }
}
