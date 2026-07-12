using Microsoft.Extensions.Logging;

namespace Benzene.Diagnostics.Timers;

public class LoggingProcessTimerFactory : IProcessTimerFactory
{
    private readonly ILogger _logger;

    public LoggingProcessTimerFactory(ILogger<LoggingProcessTimer> logger)
    {
        _logger = logger;
    }

    public IProcessTimer Create(string timerName)
    {
        return new LoggingProcessTimer(timerName, _logger);
    }
}
