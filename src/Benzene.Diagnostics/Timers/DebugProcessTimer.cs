using System.Diagnostics;

namespace Benzene.Diagnostics.Timers;

public sealed class DebugProcessTimer : IProcessTimer
{
    private readonly Stopwatch _stopwatch;
    private readonly string _timerName;

    public DebugProcessTimer(string timerName)
    {
        _stopwatch = new Stopwatch();
        _timerName = timerName;
        Debug.WriteLine($"{timerName} started");
        _stopwatch.Start();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        var time = _stopwatch.ElapsedMilliseconds;
        Debug.WriteLine($"{_timerName} took {time}ms to complete");
    }

    public void SetTag(string key, string value)
    {
        Debug.WriteLine($"{_timerName} tagged as {key}:{value}");
    }
}
