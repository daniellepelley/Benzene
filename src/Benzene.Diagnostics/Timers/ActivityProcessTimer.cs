using System.Diagnostics;

namespace Benzene.Diagnostics.Timers;

/// <summary>
/// An <see cref="IProcessTimer"/> backed by a real <see cref="Activity"/>, superseding the vendor-specific
/// timer backends (Datadog/Zipkin/X-Ray/OpenTelemetry). Kept as a source-compatible adapter for existing
/// <c>UseTimer(string)</c> call sites — construction starts the <see cref="Activity"/>, <see cref="Dispose"/> ends it.
/// </summary>
public sealed class ActivityProcessTimer : IProcessTimer
{
    private readonly Activity? _activity;

    public ActivityProcessTimer(string timerName)
    {
        _activity = BenzeneDiagnostics.ActivitySource.StartActivity(timerName);
    }

    public void SetTag(string key, string value)
    {
        _activity?.SetTag(key, value);
    }

    public void Dispose()
    {
        _activity?.Dispose();
    }
}

/// <summary>Creates <see cref="ActivityProcessTimer"/> instances. The default <see cref="IProcessTimerFactory"/> registered by <see cref="DependencyInjectionExtensions.AddDiagnostics"/>.</summary>
public class ActivityProcessTimerFactory : IProcessTimerFactory
{
    public IProcessTimer Create(string timerName)
    {
        return new ActivityProcessTimer(timerName);
    }
}
