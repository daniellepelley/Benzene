using Benzene.Abstractions.Messages;

namespace Benzene.Abstractions.MessageHandlers.Mappers;

/// <summary>
/// A transport context that can carry a preset <see cref="ITopic"/>, set by middleware earlier in
/// the pipeline (e.g. <c>PresetTopicMiddleware{TContext}</c> in <c>Benzene.Core.MessageHandlers</c>)
/// to force routing for a whole queue/subscription regardless of what the message itself carries.
/// </summary>
public interface IHasPresetTopic
{
    /// <summary>
    /// Gets or sets the preset topic for this context, or <c>null</c> if none has been set. When
    /// set, a <c>PresetTopicMessageTopicGetter{TContext}</c> returns this in preference to whatever
    /// the underlying transport-specific <see cref="IMessageTopicGetter{TContext}"/> would resolve.
    /// </summary>
    ITopic? PresetTopic { get; set; }
}
