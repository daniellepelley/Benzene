using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Decorates a transport's <see cref="IMessageTopicGetter{TContext}"/>, preferring
/// <see cref="IHasPresetTopic.PresetTopic"/> when it has been set on the context (by
/// <see cref="PresetTopicMiddleware{TContext}"/>, via the <c>UsePresetTopic</c> pipeline extension)
/// and falling back to the transport's own topic extraction otherwise.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type, which must be able to carry a preset topic.</typeparam>
/// <remarks>
/// Every transport that supports preset topics registers this wrapping its own real getter as the
/// default <see cref="IMessageTopicGetter{TContext}"/>, so a pipeline that never calls
/// <c>UsePresetTopic</c> behaves exactly as it did before this type existed.
/// </remarks>
public class PresetTopicMessageTopicGetter<TContext> : IMessageTopicGetter<TContext>
    where TContext : IHasPresetTopic
{
    private readonly IMessageTopicGetter<TContext> _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresetTopicMessageTopicGetter{TContext}"/> class.
    /// </summary>
    /// <param name="inner">The transport's own topic getter, used when no preset topic is set.</param>
    public PresetTopicMessageTopicGetter(IMessageTopicGetter<TContext> inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public ITopic? GetTopic(TContext context) => context.PresetTopic ?? _inner.GetTopic(context);
}
