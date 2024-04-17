using Benzene.Abstractions.Logging;
using Benzene.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Benzene.Microsoft.Logging;

public class MicrosoftBenzeneLogAppender : IBenzeneLogAppender
{
    private readonly ILogger<BenzeneLogger> _logger;

    public MicrosoftBenzeneLogAppender(ILogger<BenzeneLogger> logger)
    {
        _logger = logger;
    }

    public void Log(BenzeneLogLevel benzeneLogLevel, Exception? exception, string? message, params object?[] args)
    {
        _logger.Log(Convert(benzeneLogLevel), 0, exception, message, args);
    }

    private static LogLevel Convert(BenzeneLogLevel benzeneLogLevel)
    {
        return benzeneLogLevel switch
        {
            BenzeneLogLevel.Critical => LogLevel.Critical,
            BenzeneLogLevel.Debug => LogLevel.Debug,
            BenzeneLogLevel.Error => LogLevel.Error,
            BenzeneLogLevel.Trace => LogLevel.Trace,
            BenzeneLogLevel.Information => LogLevel.Information,
            BenzeneLogLevel.Warning => LogLevel.Warning,
            _ => LogLevel.None
        };
    }
}
