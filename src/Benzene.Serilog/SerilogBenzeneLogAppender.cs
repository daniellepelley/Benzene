using Benzene.Abstractions.Logging;
using Serilog;
using Serilog.Events;

namespace Benzene.Serilog;

public abstract class SerilogBenzeneLogAppender : IBenzeneLogAppender
{
    private readonly ILogger _logger;

    public SerilogBenzeneLogAppender(ILogger logger)
    {
        _logger = logger;
    }

    public void Log(BenzeneLogLevel benzeneLogLevel, Exception? exception, string? message, params object?[] args)
    {
        _logger.Write(Convert(benzeneLogLevel), exception, message ?? "", args);
    }

    private static LogEventLevel Convert(BenzeneLogLevel benzeneLogLevel)
    {
        return benzeneLogLevel switch
        {
            BenzeneLogLevel.Critical => LogEventLevel.Fatal,
            BenzeneLogLevel.Debug => LogEventLevel.Debug,
            BenzeneLogLevel.Error => LogEventLevel.Error,
            BenzeneLogLevel.Trace => LogEventLevel.Verbose,
            BenzeneLogLevel.Information => LogEventLevel.Information,
            BenzeneLogLevel.Warning => LogEventLevel.Warning,
            _ => LogEventLevel.Information
        };
    }
}
