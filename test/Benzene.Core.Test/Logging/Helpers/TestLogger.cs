using System;
using System.Collections.Generic;
using Benzene.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Benzene.Test.Logging.Helpers;
public class TestLogger : ILogger<BenzeneLogger>
{
    private readonly List<LogLevel> _logList = new();
    public LogLevel[] Logs => _logList.ToArray();

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        _logList.Add(logLevel);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return new NullDisposable();
    }
}
