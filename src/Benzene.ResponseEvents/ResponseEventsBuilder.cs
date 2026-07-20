using Benzene.Abstractions.Results;

namespace Benzene.ResponseEvents;

/// <summary>
/// Fluent configuration surface for one pipeline's response-event mappings, passed to the callback
/// of <c>UseResponseEvents(...)</c> (<see cref="ResponseEventsExtensions.UseResponseEvents"/>).
/// </summary>
public sealed class ResponseEventsBuilder
{
    private readonly List<IResponseEventMapping> _mappings = new();
    private PublishFailureMode _publishFailureMode = PublishFailureMode.FailMessage;

    /// <summary>
    /// Maps one source topic's successful, payload-carrying responses to an event topic.
    /// </summary>
    /// <param name="sourceTopic">The topic whose handler responses to republish.</param>
    /// <param name="eventTopic">The topic to publish the response payload on.</param>
    /// <param name="when">Optional predicate over the handler's result replacing the default
    /// <c>IsSuccessful</c> check (a non-null payload is always required).</param>
    /// <returns>This builder, for chaining.</returns>
    public ResponseEventsBuilder Map(string sourceTopic, string eventTopic, Func<IBenzeneResult, bool>? when = null)
    {
        _mappings.Add(new ExplicitResponseEventMapping(sourceTopic, eventTopic, null, when));
        return this;
    }

    /// <summary>
    /// Maps one source topic's successful responses to an event topic, declaring the event payload
    /// type - which surfaces the event in generated specs (AsyncAPI / event-service documents) via
    /// <see cref="ResponseEventCatalog"/> - and optionally reshaping the payload.
    /// </summary>
    /// <typeparam name="TPayload">The event payload type (the handler's response type, unless <paramref name="project"/> reshapes it).</typeparam>
    /// <param name="sourceTopic">The topic whose handler responses to republish.</param>
    /// <param name="eventTopic">The topic to publish on.</param>
    /// <param name="when">Optional predicate over the handler's result replacing the default
    /// <c>IsSuccessful</c> check (a non-null payload is always required).</param>
    /// <param name="project">Optional projection from the response payload to the event payload;
    /// returning <c>null</c> skips the publish for that message.</param>
    /// <returns>This builder, for chaining.</returns>
    public ResponseEventsBuilder Map<TPayload>(string sourceTopic, string eventTopic,
        Func<IBenzeneResult, bool>? when = null, Func<TPayload, object?>? project = null)
    {
        _mappings.Add(new ExplicitResponseEventMapping(sourceTopic, eventTopic, typeof(TPayload), when,
            project == null ? null : payload => payload is TPayload typed ? project(typed) : payload));
        return this;
    }

    /// <summary>
    /// Adds the CRUD naming convention (<see cref="CrudConventionResponseEventMapping"/>):
    /// <c>X:create</c>/<c>update</c>/<c>delete</c> handled with status
    /// <c>Created</c>/<c>Updated</c>/<c>Deleted</c> publishes the payload on <c>X:created</c>/
    /// <c>updated</c>/<c>deleted</c>.
    /// </summary>
    /// <returns>This builder, for chaining.</returns>
    public ResponseEventsBuilder MapCrudConvention()
    {
        _mappings.Add(new CrudConventionResponseEventMapping());
        return this;
    }

    /// <summary>
    /// Adds a fully custom mapping rule - the escape hatch when neither explicit maps nor the CRUD
    /// convention fit (multi-topic rules, payload-dependent topics, ...).
    /// </summary>
    /// <param name="mapping">The mapping to add.</param>
    /// <returns>This builder, for chaining.</returns>
    public ResponseEventsBuilder Add(IResponseEventMapping mapping)
    {
        _mappings.Add(mapping);
        return this;
    }

    /// <summary>
    /// Sets what happens when publishing a matched event fails. Defaults to
    /// <see cref="PublishFailureMode.FailMessage"/>.
    /// </summary>
    /// <param name="mode">The failure mode for this pipeline.</param>
    /// <returns>This builder, for chaining.</returns>
    public ResponseEventsBuilder OnPublishFailure(PublishFailureMode mode)
    {
        _publishFailureMode = mode;
        return this;
    }

    /// <summary>Builds the immutable mapping set.</summary>
    /// <returns>The pipeline's mappings and failure policy.</returns>
    internal ResponseEventMappings Build()
    {
        return new ResponseEventMappings(_mappings.ToArray(), _publishFailureMode);
    }
}
