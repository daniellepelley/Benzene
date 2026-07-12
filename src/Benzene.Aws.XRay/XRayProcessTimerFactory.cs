using Benzene.Diagnostics.Timers;

namespace Benzene.Aws.XRay;

/// <summary>
/// Creates <see cref="XRayProcessProcessTimer"/> instances, recording each timed operation as an AWS
/// X-Ray subsegment.
/// </summary>
public class XRayProcessTimerFactory : IProcessTimerFactory
{
    /// <summary>
    /// Creates a new X-Ray subsegment timer.
    /// </summary>
    /// <param name="timerName">The name of the X-Ray subsegment.</param>
    /// <returns>The created timer.</returns>
    public IProcessTimer Create(string timerName)
    {
        return new XRayProcessProcessTimer(timerName);
    }
}
