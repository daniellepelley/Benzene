using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Serialization;

namespace Benzene.Azure.Function.Timer;

/// <summary>
/// The transport's own topic getter for timer ticks - which always returns <c>null</c>, because a
/// tick carries no message; the topic is the scheduled job's identity, declared per pipeline with
/// <c>UsePresetTopic("nightly-cleanup")</c> (via the <c>PresetTopicMessageTopicGetter</c> this
/// getter is wrapped in).
/// </summary>
public class TimerMessageTopicGetter : IMessageTopicGetter<TimerContext>
{
    /// <summary>
    /// Always returns <c>null</c> - the topic comes from <c>UsePresetTopic(...)</c>.
    /// </summary>
    /// <param name="context">The timer context.</param>
    /// <returns><c>null</c>.</returns>
    public ITopic? GetTopic(TimerContext context) => null;
}

/// <summary>
/// Provides the headers for a timer tick - always empty; a tick carries no message metadata beyond
/// the schedule info in the body.
/// </summary>
public class TimerMessageHeadersGetter : IMessageHeadersGetter<TimerContext>
{
    /// <summary>
    /// Gets an empty header dictionary.
    /// </summary>
    /// <param name="context">The timer context.</param>
    /// <returns>An empty dictionary.</returns>
    public IDictionary<string, string> GetHeaders(TimerContext context)
    {
        return new Dictionary<string, string>();
    }
}

/// <summary>
/// Provides the message body for a timer tick: the serialized <see cref="TimerTriggerInfo"/>, so a
/// handler whose request type mirrors it (or any subset of its properties) receives the schedule
/// information, and a handler with an empty request type binds cleanly too.
/// </summary>
public class TimerMessageBodyGetter : IMessageBodyGetter<TimerContext>
{
    private readonly JsonSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerMessageBodyGetter"/> class.
    /// </summary>
    /// <param name="serializer">The serializer used to write the timer info as the body.</param>
    public TimerMessageBodyGetter(JsonSerializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    /// Gets the tick's <see cref="TimerTriggerInfo"/> serialized as JSON.
    /// </summary>
    /// <param name="context">The timer context to extract the body from.</param>
    /// <returns>The message body.</returns>
    public string GetBody(TimerContext context)
    {
        return _serializer.Serialize(context.Timer);
    }
}

/// <summary>
/// Records a message handler's outcome onto <see cref="TimerContext.MessageResult"/> - recorded
/// for middleware/diagnostics; a tick has no caller to answer.
/// </summary>
public class TimerMessageMessageHandlerResultSetter : MessageMessageHandlerResultSetterBase<TimerContext>;
