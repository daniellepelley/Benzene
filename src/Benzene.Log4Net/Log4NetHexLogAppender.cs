using Benzene.Abstractions.Logging;
using Benzene.Core.Logging;
using log4net;

namespace Benzene.Log4Net;

public class Log4NetBenzeneLogAppender : IBenzeneLogAppender
{
    private readonly ILog _log = LogManager.GetLogger(typeof(BenzeneLogger));

    public void Log(BenzeneLogLevel benzeneLogLevel, Exception? exception, string? message, params object?[] args)
    {
        switch (benzeneLogLevel)
        {
            case BenzeneLogLevel.Critical:
                _log.FatalFormat(message, args);
                 break;
            case BenzeneLogLevel.Debug:
            case BenzeneLogLevel.Trace:
                _log.DebugFormat(message, args);
                 break;
            case BenzeneLogLevel.Error:
                _log.ErrorFormat(message, args);
                 break;
            case BenzeneLogLevel.Information:
                _log.InfoFormat(message, args);
                 break;
            case BenzeneLogLevel.Warning:
                _log.WarnFormat(message, args);
                 break;
        }
    }
}
