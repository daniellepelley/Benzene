using Amazon.XRay.Recorder.Core;
using Benzene.Diagnostics.Timers;

namespace Benzene.Aws.XRay;

public sealed class XRayProcessProcessTimer : IProcessTimer
{
    public XRayProcessProcessTimer(string timerName)
    {
        if (AWSXRayRecorder.Instance.IsTracingDisabled())
            return;
        AWSXRayRecorder.Instance.BeginSubsegment(timerName);
    }

    public void Dispose()
    {
        if (AWSXRayRecorder.Instance.IsTracingDisabled())
            return;
        AWSXRayRecorder.Instance.EndSubsegment();
    }

    public void SetTag(string key, string value)
    {
        if (AWSXRayRecorder.Instance.IsTracingDisabled())
            return;
        AWSXRayRecorder.Instance.AddAnnotation(key, value);
    }
}
