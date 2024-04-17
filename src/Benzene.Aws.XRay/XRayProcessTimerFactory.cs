using System;
using Benzene.Diagnostics.Timers;

namespace Benzene.Aws.XRay;

public class XRayProcessTimerFactory : IProcessTimerFactory
{
    public IProcessTimer Create(string timerName)
    {
        return new XRayProcessProcessTimer(timerName);
    }
}
