using Datadog.Trace;
using Benzene.Diagnostics.Timers;

namespace Benzene.Datadog;

public sealed class DatadogProcessTimer : IProcessTimer
{
    private readonly IScope _dataDogScope;

    public DatadogProcessTimer(string timerName)
    {
        _dataDogScope = Tracer.Instance.StartActive(timerName);
    }

    public void Dispose()
    {
        _dataDogScope.Dispose();
    }

    public void SetTag(string key, string value)
    {
        _dataDogScope.Span.SetTag(key, value);
    }
}
