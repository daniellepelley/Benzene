using Amazon.XRay.Recorder.Core;
using Benzene.Aws.XRay;
using Benzene.Diagnostics.Timers;
using Xunit;

namespace Benzene.Test.Aws.XRay;

public class XRayProcessProcessTimerTest
{
    [Fact]
    public void Construct_NoActiveSegment_DoesNotThrow()
    {
        using var timer = new XRayProcessProcessTimer("test-segment");

        Assert.IsAssignableFrom<IProcessTimer>(timer);
    }

    [Fact]
    public void SetTag_NoActiveSegment_DoesNotThrow()
    {
        using var timer = new XRayProcessProcessTimer("test-segment");

        timer.SetTag("key", "value");
    }

    [Fact]
    public void Dispose_NoActiveSegment_DoesNotThrow()
    {
        var timer = new XRayProcessProcessTimer("test-segment");

        timer.Dispose();
    }

    [Fact]
    public void Construct_ActiveSegment_CreatesSubsegment()
    {
        AWSXRayRecorder.Instance.BeginSegment("test-segment-root");
        try
        {
            using var timer = new XRayProcessProcessTimer("test-subsegment");

            timer.SetTag("key", "value");
        }
        finally
        {
            AWSXRayRecorder.Instance.EndSegment();
        }
    }
}
