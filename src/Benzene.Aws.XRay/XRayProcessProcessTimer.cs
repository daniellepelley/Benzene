using Amazon.XRay.Recorder.Core;
using Benzene.Diagnostics.Timers;

namespace Benzene.Aws.XRay;

/// <summary>
/// Times a unit of work as an AWS X-Ray subsegment, from construction until disposal.
/// </summary>
/// <remarks>
/// All operations are no-ops if X-Ray tracing is disabled (e.g. no active segment for the current
/// execution context), so this is safe to use unconditionally.
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
        AWSXRayRecorder.Instance.BeginSubsegment(timerName);
    }

    /// <summary>
    /// Ends the X-Ray subsegment.
    /// </summary>
    public void Dispose()
    {
        if (AWSXRayRecorder.Instance.IsTracingDisabled())
            return;
        AWSXRayRecorder.Instance.EndSubsegment();
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
        AWSXRayRecorder.Instance.AddAnnotation(key, value);
    }
}
