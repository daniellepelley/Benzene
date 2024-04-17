using System;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;

namespace Benzene.Aws.XRay;

public class XRayContext : IDisposable
{
    private readonly bool _isRequired;

    public XRayContext()
    {
        _isRequired = !AWSXRayRecorder.Instance.IsEntityPresent();
        if (_isRequired)
        {
            try
            {
                AWSXRayRecorder.Instance.BeginSegment("Trace", TraceId.NewId());
            }
            catch
            {
                _isRequired = false;
            }
        }
    }

    public void Dispose()
    {
        if (_isRequired)
        {
            AWSXRayRecorder.Instance.EndSegment();
        }
    }
}
