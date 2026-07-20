using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// A host self-check on this process's memory usage. Reports <see cref="HealthCheckStatus.Failed"/>
/// at or above a hard maximum, an optional <see cref="HealthCheckStatus.Warning"/> at or above a soft
/// threshold (degraded-but-not-fatal, does not flip aggregate <c>IsHealthy</c>), otherwise healthy.
/// Measures the process working set - the physical memory a container/host OOM-killer watches - and
/// includes the managed-heap size and the runtime's reported memory limit in the result data for
/// diagnostics. Unlike <c>DiskHealthCheck</c>, higher is worse, so the thresholds are ceilings.
/// </summary>
/// <remarks>
/// This lives in <c>Benzene.HealthChecks</c> (not a dedicated <c>Benzene.HealthChecks.Memory</c>
/// package like Disk/Tcp) because it has no external dependency - it reads only <see cref="Environment"/>
/// and <see cref="GC"/>. Register it via <c>AddMemoryCheck(...)</c>.
/// </remarks>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _maximumBytes;
    private readonly long? _warningBytes;
    private readonly Func<long> _currentBytes;

    /// <summary>Initializes a new instance measuring <see cref="Environment.WorkingSet"/>.</summary>
    /// <param name="maximumBytes">At or above this many bytes of working set the check fails.</param>
    /// <param name="warningBytes">Optional soft threshold: at or above this (but below the maximum) the check warns.</param>
    public MemoryHealthCheck(long maximumBytes, long? warningBytes = null)
        : this(maximumBytes, warningBytes, () => Environment.WorkingSet)
    {
    }

    // Testable seam: inject the measured value so a test's Failed/Warning boundary doesn't depend on
    // the host's real, drifting working set (visible to Benzene.Test via InternalsVisibleTo).
    internal MemoryHealthCheck(long maximumBytes, long? warningBytes, Func<long> currentBytes)
    {
        _maximumBytes = maximumBytes;
        _warningBytes = warningBytes;
        _currentBytes = currentBytes;
    }

    /// <inheritdoc />
    public string Type => "Memory";

    /// <inheritdoc />
    public Task<IHealthCheckResult> ExecuteAsync()
    {
        try
        {
            var workingSet = _currentBytes();
            var gcInfo = GC.GetGCMemoryInfo();
            var data = new Dictionary<string, object>
            {
                { "WorkingSetBytes", workingSet },
                { "ManagedHeapBytes", GC.GetTotalMemory(false) },
                { "MaximumBytes", _maximumBytes },
                { "GcTotalAvailableBytes", gcInfo.TotalAvailableMemoryBytes },
                { "GcHighLoadThresholdBytes", gcInfo.HighMemoryLoadThresholdBytes },
            };

            if (workingSet >= _maximumBytes)
            {
                return Task.FromResult(HealthCheckResult.CreateInstance(false, Type, data));
            }

            if (_warningBytes.HasValue && workingSet >= _warningBytes.Value)
            {
                return Task.FromResult(HealthCheckResult.CreateWarning(Type, data));
            }

            return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object> { { "Error", ex.GetType().Name } }));
        }
    }
}
