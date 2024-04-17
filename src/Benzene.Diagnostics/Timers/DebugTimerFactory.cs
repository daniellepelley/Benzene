namespace Benzene.Diagnostics.Timers;

public class DebugTimerFactory : IProcessTimerFactory
{
    public IProcessTimer Create(string timerName)
    {
        return new DebugProcessTimer(timerName);
    }
}
