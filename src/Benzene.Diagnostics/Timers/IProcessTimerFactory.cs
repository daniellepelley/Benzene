namespace Benzene.Diagnostics.Timers;

public interface IProcessTimerFactory
{
    public IProcessTimer Create(string timerName);
}
