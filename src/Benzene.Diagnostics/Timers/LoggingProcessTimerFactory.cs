using Benzene.Abstractions.Logging;

namespace Benzene.Diagnostics.Timers;

public class LoggingProcessTimerFactory : IProcessTimerFactory
{
    private readonly IBenzeneLogger _logger;

    public LoggingProcessTimerFactory(IBenzeneLogger logger)
    {
        _logger = logger;
    }

    public IProcessTimer Create(string timerName)
    {
        return new LoggingProcessTimer(timerName, _logger);
    }
}
