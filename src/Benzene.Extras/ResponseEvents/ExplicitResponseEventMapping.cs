using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;

namespace Benzene.Extras.ResponseEvents;

/// <summary>
/// The standard <see cref="IResponseEventMapping"/>: a fixed source topic mapped to a fixed event
/// topic. By default it fires when the handler's result is successful and carries a payload; an
/// optional <c>when</c> predicate replaces the status check (the non-null-payload requirement
/// always applies - there is no event to publish without one), and an optional projector reshapes
/// the payload before publishing (returning <c>null</c> from the projector skips the publish).
/// </summary>
public sealed class ExplicitResponseEventMapping : IResponseEventMapping
{
    private readonly Func<IBenzeneResult, bool>? _when;
    private readonly Func<object, object?>? _projectPayload;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExplicitResponseEventMapping"/> class.
    /// </summary>
    /// <param name="sourceTopic">The topic id whose handler responses this mapping applies to (compared case-insensitively).</param>
    /// <param name="eventTopic">The topic id the event is published on.</param>
    /// <param name="payloadType">The declared event payload type, for spec generation. Optional.</param>
    /// <param name="when">Predicate over the handler's result deciding whether to publish. Defaults to <c>result.IsSuccessful</c>.</param>
    /// <param name="projectPayload">Optional projection from the response payload to the event payload. Returning <c>null</c> skips the publish.</param>
    public ExplicitResponseEventMapping(string sourceTopic, string eventTopic, Type? payloadType = null,
        Func<IBenzeneResult, bool>? when = null, Func<object, object?>? projectPayload = null)
    {
        SourceTopic = sourceTopic;
        EventTopic = eventTopic;
        PayloadType = payloadType;
        _when = when;
        _projectPayload = projectPayload;
    }

    /// <inheritdoc />
    public string SourceTopic { get; }

    /// <inheritdoc />
    public string EventTopic { get; }

    /// <inheritdoc />
    public Type? PayloadType { get; }

    /// <inheritdoc />
    public string Description =>
        $"{SourceTopic} -> {EventTopic}" +
        (PayloadType != null ? $" ({PayloadType.Name})" : string.Empty) +
        (_when != null ? " [conditional]" : string.Empty);

    /// <inheritdoc />
    public ResponseEventPublication? Resolve(ITopic sourceTopic, IBenzeneResult result)
    {
        if (!string.Equals(sourceTopic.Id, SourceTopic, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!(_when?.Invoke(result) ?? result.IsSuccessful))
        {
            return null;
        }

        var payload = result.PayloadAsObject;
        if (payload == null)
        {
            return null;
        }

        if (_projectPayload != null)
        {
            payload = _projectPayload(payload);
            if (payload == null)
            {
                return null;
            }
        }

        return new ResponseEventPublication(EventTopic, payload);
    }
}
