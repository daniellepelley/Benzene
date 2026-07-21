using System;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Exceptions;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;

namespace Benzene.Aws.Lambda.XRay;

/// <summary>
/// Wraps a single middleware in an AWS X-Ray subsegment (named after the middleware), so every pipeline
/// stage appears as a timed subsegment nested under the Lambda's X-Ray segment. The direct-to-X-Ray
/// counterpart of <c>Benzene.Diagnostics.ActivityMiddlewareDecorator</c> - it talks to the AWS X-Ray SDK
/// (<see cref="AWSXRayRecorder"/>) rather than emitting an <c>Activity</c> span for an OpenTelemetry
/// exporter to ship, so the subsegments land in X-Ray directly with no OTLP collector in between.
/// </summary>
/// <remarks>
/// A no-op when there is no active X-Ray segment in context - i.e. off Lambda, or on Lambda with active
/// tracing turned off - mirroring how <c>ActivitySource.StartActivity</c> returns <c>null</c> when
/// nothing is listening. On Lambda with active tracing on, the runtime populates the X-Ray context per
/// invocation (from <c>_X_AMZN_TRACE_ID</c>), so <c>BeginSubsegment</c> nests under the Lambda's own
/// segment automatically - which is what makes the middleware breakdown show up inside the same X-Ray
/// trace as the <c>AWS::Lambda::Function</c> segment.
/// </remarks>
public class XRayMiddlewareDecorator<TContext> : IMiddleware<TContext>
{
    private readonly IMiddleware<TContext> _inner;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>Initializes a new instance of the <see cref="XRayMiddlewareDecorator{TContext}"/> class.</summary>
    public XRayMiddlewareDecorator(IMiddleware<TContext> inner, IServiceResolver serviceResolver)
    {
        _inner = inner;
        _serviceResolver = serviceResolver;
    }

    /// <inheritdoc />
    public string Name => _inner.Name;

    /// <inheritdoc />
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var recorder = AWSXRayRecorder.Instance;
        if (!TryBeginSubsegment(recorder))
        {
            // No active X-Ray segment (not on Lambda, or active tracing off) - trace nothing, just run on.
            // (Kept as a single async method rather than a sync fast-path: the X-Ray SDK's subsegment
            // Begin/End must sit in the same async-flow/ExecutionContext, and on Lambda with active tracing
            // a segment is always present so this branch is off the production hot path anyway.)
            await _inner.HandleAsync(context, next);
            return;
        }

        try
        {
            Tag(recorder, context);
            await _inner.HandleAsync(context, next);
        }
        catch (Exception ex)
        {
            // Record the exception so the subsegment shows as a fault at the failing stage, then rethrow
            // untouched - this only observes, it doesn't handle.
            Safe(() => recorder.AddException(ex));
            throw;
        }
        finally
        {
            Safe(() => recorder.EndSubsegment());
        }
    }

    private bool TryBeginSubsegment(IAWSXRayRecorder recorder)
    {
        try
        {
            recorder.BeginSubsegment(_inner.Name);
            return true;
        }
        catch (EntityNotAvailableException)
        {
            // No parent segment in context - off Lambda or tracing disabled. Behave as a no-op.
            return false;
        }
    }

    private void Tag(IAWSXRayRecorder recorder, TContext context)
    {
        // X-Ray annotation keys must be alphanumeric/underscore (dots are rejected), so these use
        // benzene_ keys rather than the benzene.* names the Activity decorator uses. Annotations are
        // indexed and filterable in the X-Ray console.
        var transport = _serviceResolver.TryGetService<ICurrentTransport>();
        if (transport is not null)
        {
            Safe(() => recorder.AddAnnotation("benzene_transport", transport.Name));
        }

        var topic = _serviceResolver.TryGetService<IMessageGetter<TContext>>()?.GetTopic(context);
        if (topic is not null && !string.IsNullOrEmpty(topic.Id))
        {
            Safe(() => recorder.AddAnnotation("benzene_topic", topic.Id));
            if (!string.IsNullOrEmpty(topic.Version))
            {
                Safe(() => recorder.AddAnnotation("benzene_version", topic.Version));
            }

            var handler = _serviceResolver.TryGetService<IMessageHandlerDefinitionLookUp>()?.FindHandler(topic);
            if (handler is not null)
            {
                Safe(() => recorder.AddAnnotation("benzene_handler", handler.HandlerType.Name));
            }
        }
    }

    // The X-Ray context can legitimately vanish mid-stage (e.g. the facade segment is torn down at
    // invocation end); treat that as "nothing to annotate/close" rather than letting an observability
    // concern throw into the real pipeline.
    private static void Safe(Action action)
    {
        try
        {
            action();
        }
        catch (EntityNotAvailableException)
        {
        }
    }
}
