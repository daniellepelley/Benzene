using Benzene.Diagnostics.Timers;

namespace Benzene.OpenTelemetry;

public class OpenTelemetryProcessTimerFactory : IProcessTimerFactory
{
    public IProcessTimer Create(string timerName)
    {
        return new OpenTelemetryProcessTimer(timerName);
    }
}
