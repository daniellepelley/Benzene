using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Extras.ResponseEvents;

/// <summary>
/// The CRUD convention rule (the behavior <c>Benzene.Extras.Broadcast</c> hardwired, expressed as
/// one opt-in <see cref="IResponseEventMapping"/>): a topic whose last <c>:</c>-segment is
/// <c>create</c>/<c>update</c>/<c>delete</c>, handled with the matching result status
/// (<c>Created</c>/<c>Updated</c>/<c>Deleted</c>) and a payload, publishes that payload on the
/// past-tense topic (<c>order:create</c> -> <c>order:created</c>). Added via
/// <see cref="ResponseEventsBuilder.MapCrudConvention"/>.
/// </summary>
public sealed class CrudConventionResponseEventMapping : IResponseEventMapping
{
    private static readonly IReadOnlyDictionary<string, string> VerbToStatus =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["create"] = BenzeneResultStatus.Created,
            ["update"] = BenzeneResultStatus.Updated,
            ["delete"] = BenzeneResultStatus.Deleted,
        };

    /// <inheritdoc />
    public string Description =>
        "CRUD convention: <entity>:create|update|delete -> <entity>:created|updated|deleted on Created/Updated/Deleted";

    /// <inheritdoc />
    public string? SourceTopic => null;

    /// <inheritdoc />
    public string? EventTopic => null;

    /// <inheritdoc />
    public Type? PayloadType => null;

    /// <inheritdoc />
    public ResponseEventPublication? Resolve(ITopic sourceTopic, IBenzeneResult result)
    {
        var verb = sourceTopic.Id.Split(':').Last();
        if (!VerbToStatus.TryGetValue(verb, out var requiredStatus) || result.Status != requiredStatus)
        {
            return null;
        }

        var payload = result.PayloadAsObject;
        return payload == null
            ? null
            : new ResponseEventPublication($"{sourceTopic.Id}d", payload);
    }
}
