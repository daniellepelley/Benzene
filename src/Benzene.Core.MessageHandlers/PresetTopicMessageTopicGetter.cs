using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Decorates a transport's <see cref="IMessageTopicGetter{TContext}"/>, preferring
/// <see cref="PresetTopicHolder.PresetTopic"/> when it has been set for the current message (by
/// <see cref="PresetTopicMiddleware{TContext}"/>, via the <c>UsePresetTopic</c> pipeline extension)
/// and falling back to the transport's own topic extraction otherwise.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type. No special shape is required - this getter never touches it beyond the inner getter's own needs.</typeparam>
/// <remarks>
/// Every transport that supports preset topics registers this wrapping its own real getter as the
/// default <see cref="IMessageTopicGetter{TContext}"/>, so a pipeline that never calls
/// <c>UsePresetTopic</c> behaves exactly as it did before this type existed - the holder it reads
/// is a fresh, scoped-per-message instance whose <see cref="PresetTopicHolder.PresetTopic"/> stays
/// <c>null</c> unless that message's own pipeline included a <see cref="PresetTopicMiddleware{TContext}"/>.
/// </remarks>
public class PresetTopicMessageTopicGetter<TContext> : IMessageTopicGetter<TContext>
{
    private readonly IMessageTopicGetter<TContext> _inner;
    private readonly PresetTopicHolder _holder;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresetTopicMessageTopicGetter{TContext}"/> class.
    /// </summary>
    /// <param name="inner">The transport's own topic getter, used when no preset topic is set.</param>
    /// <param name="holder">The current message's preset-topic holder, resolved from the same DI scope as this getter.</param>
    public PresetTopicMessageTopicGetter(IMessageTopicGetter<TContext> inner, PresetTopicHolder holder)
    {
        _inner = inner;
        _holder = holder;
    }

    /// <inheritdoc />
    public ITopic? GetTopic(TContext context) => _holder.PresetTopic ?? _inner.GetTopic(context);
}
