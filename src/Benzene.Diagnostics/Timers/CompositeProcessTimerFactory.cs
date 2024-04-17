namespace Benzene.Diagnostics.Timers;

public class CompositeProcessTimerFactory : IProcessTimerFactory
{
    private readonly IProcessTimerFactory[] _processTimerFactories;

    public CompositeProcessTimerFactory(params IProcessTimerFactory[] processTimerFactories)
    {
        _processTimerFactories = processTimerFactories;
    }

    public IProcessTimer Create(string timerName)
    {
        return new CompositeProcessTimer(_processTimerFactories.Select(x => x.Create(timerName)));
    }
}
