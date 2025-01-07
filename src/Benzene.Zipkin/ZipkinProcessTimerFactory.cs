using Benzene.Diagnostics.Timers;

namespace Benzene.Zipkin;

public class ZipkinProcessTimerFactory : IProcessTimerFactory
{
    public IProcessTimer Create(string timerName)
    {
        return new ZipkinProcessTimer(timerName);
    }
}
