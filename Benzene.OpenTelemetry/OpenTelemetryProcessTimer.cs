using Benzene.Diagnostics.Timers;
using OpenTelemetry.Trace;
public sealed class OpenTelemetryProcessTimer : IProcessTimer
{
    private readonly Tracer _tracer;
    private readonly TelemetrySpan _span;
    public OpenTelemetryProcessTimer(string timerName)
    {
        _tracer = TracerProvider.Default.GetTracer("ProcessTimer");
        _span = _tracer.StartSpan(timerName);
    }
    public void Dispose()
    {
        _span.End();
    }
    
    public void SetTag(string key, string value)
    {
        _span.SetAttribute(key, value);
    }
}
