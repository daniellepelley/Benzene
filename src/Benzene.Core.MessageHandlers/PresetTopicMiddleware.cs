using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Sets a fixed <see cref="ITopic"/> on the context before continuing the pipeline, so a
/// <see cref="PresetTopicMessageTopicGetter{TContext}"/> resolves it in preference to whatever the
/// transport itself would otherwise extract. Added via the <c>UsePresetTopic</c> pipeline extension,
/// before <c>UseMessageHandlers</c>, for one specific queue/subscription's pipeline.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type, which must be able to carry a preset topic.</typeparam>
public class PresetTopicMiddleware<TContext> : IMiddleware<TContext>
    where TContext : IHasPresetTopic
{
    private readonly ITopic _presetTopic;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresetTopicMiddleware{TContext}"/> class.
    /// </summary>
    /// <param name="presetTopic">The topic to set on every context this middleware sees.</param>
    public PresetTopicMiddleware(ITopic presetTopic)
    {
        _presetTopic = presetTopic;
    }

    /// <inheritdoc />
    public string Name => "PresetTopic";

    /// <inheritdoc />
    public Task HandleAsync(TContext context, Func<Task> next)
    {
        context.PresetTopic = _presetTopic;
        return next();
    }
}
