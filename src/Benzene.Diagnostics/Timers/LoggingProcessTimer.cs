using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Benzene.Diagnostics.Timers;

public sealed class LoggingProcessTimer : IProcessTimer
{
    private readonly ILogger _logger;
    private readonly LogLevel _logLevel = LogLevel.Trace;
    private readonly Stopwatch _stopwatch;
    private readonly string _timerName;
    private readonly Dictionary<string, string> _tags = new Dictionary<string, string>();

    public LoggingProcessTimer(string timerName, ILogger logger)
    {
        _logger = logger;
        _stopwatch = new Stopwatch();
        _timerName = timerName;
        _logger.Log(_logLevel, "{timer} started", _timerName);
        _stopwatch.Start();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        var time = _stopwatch.ElapsedMilliseconds;
        if (_tags.Count > 0)
        {
            var message = "{timer} took {milliseconds}ms to complete. Tags = " + string.Join(", ", _tags.Keys.Select(x => $"{x}:{{{x}}}"));
            var args = new object[] { _timerName, time };
            _logger.Log(_logLevel, message, args.Concat(_tags.Values).ToArray());
        }
        else
        {
            _logger.Log(_logLevel, "{timer} took {milliseconds}ms to complete", _timerName, time);
        }
    }

    public void SetTag(string key, string value)
    {
        _tags[key] = value;
    }
}
