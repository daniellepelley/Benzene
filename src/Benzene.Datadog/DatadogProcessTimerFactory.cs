using Benzene.Diagnostics.Timers;

namespace Benzene.Datadog;

public class DatadogProcessTimerFactory : IProcessTimerFactory
{
    public IProcessTimer Create(string timerName)
    {
        return new DatadogProcessTimer(timerName);
    }
}
