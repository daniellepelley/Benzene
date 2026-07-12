using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Exceptions;
using Benzene.Diagnostics.Timers;

namespace Benzene.Aws.XRay;

/// <summary>
/// Times a unit of work as an AWS X-Ray subsegment, from construction until disposal.
/// </summary>
/// <remarks>
/// All operations are no-ops if X-Ray tracing is disabled, or if there is no active X-Ray segment for
/// the current execution context (e.g. running locally without the Lambda X-Ray context, or the segment
/// not propagating across an async boundary) — the X-Ray SDK's default <c>RUNTIME_ERROR</c> context-missing
/// strategy would otherwise throw <see cref="EntityNotAvailableException"/> in that case. This makes the
/// timer safe to use unconditionally.
/// </remarks>
public sealed class XRayProcessProcessTimer : IProcessTimer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XRayProcessProcessTimer"/> class, beginning an
    /// X-Ray subsegment with the given name.
    /// </summary>
    /// <param name="timerName">The name of the X-Ray subsegment.</param>
    public XRayProcessProcessTimer(string timerName)
    {
        if (AWSXRayRecorder.Instance.IsTracingDisabled())
            return;

        try
        {
            AWSXRayRecorder.Instance.BeginSubsegment(timerName);
        }
        catch (EntityNotAvailableException)
        {
        }
    }

    /// <summary>
    /// Ends the X-Ray subsegment.
    /// </summary>
    public void Dispose()
    {
        if (AWSXRayRecorder.Instance.IsTracingDisabled())
            return;

        try
        {
            AWSXRayRecorder.Instance.EndSubsegment();
        }
        catch (EntityNotAvailableException)
        {
        }
    }

    /// <summary>
    /// Adds an annotation to the current X-Ray subsegment.
    /// </summary>
    /// <param name="key">The annotation key.</param>
    /// <param name="value">The annotation value.</param>
    public void SetTag(string key, string value)
    {
        if (AWSXRayRecorder.Instance.IsTracingDisabled())
            return;

        try
        {
            AWSXRayRecorder.Instance.AddAnnotation(key, value);
        }
        catch (EntityNotAvailableException)
        {
        }
    }
}
