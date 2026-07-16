using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Sets a fixed <see cref="ITopic"/> on the current message's <see cref="PresetTopicHolder"/> before
/// continuing the pipeline, so a <see cref="PresetTopicMessageTopicGetter{TContext}"/> resolves it in
/// preference to whatever the transport itself would otherwise extract. Added via the
/// <c>UsePresetTopic</c> pipeline extension, before <c>UseMessageHandlers</c>, for one specific
/// queue/subscription's pipeline.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type. No special shape is required - this middleware never touches it.</typeparam>
public class PresetTopicMiddleware<TContext> : IMiddleware<TContext>
{
    private readonly PresetTopicHolder _holder;
    private readonly ITopic _presetTopic;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresetTopicMiddleware{TContext}"/> class.
    /// </summary>
    /// <param name="holder">The current message's preset-topic holder, resolved from the same DI scope <see cref="PresetTopicMessageTopicGetter{TContext}"/> reads it from.</param>
    /// <param name="presetTopic">The topic to set for every message this middleware sees.</param>
    public PresetTopicMiddleware(PresetTopicHolder holder, ITopic presetTopic)
    {
        _holder = holder;
        _presetTopic = presetTopic;
    }

    /// <inheritdoc />
    public string Name => "PresetTopic";

    /// <inheritdoc />
    public Task HandleAsync(TContext context, Func<Task> next)
    {
        _holder.PresetTopic = _presetTopic;
        return next();
    }
}
