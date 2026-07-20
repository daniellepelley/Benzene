using System.IO;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Disk;

/// <summary>
/// A host self-check on free disk space for the drive containing a given path. Reports
/// <see cref="HealthCheckStatus.Failed"/> below a hard minimum, an optional
/// <see cref="HealthCheckStatus.Warning"/> below a soft threshold (degraded-but-not-fatal, does not
/// flip aggregate <c>IsHealthy</c>), otherwise healthy.
/// </summary>
public class DiskHealthCheck : IHealthCheck
{
    private readonly string _path;
    private readonly long _minimumFreeBytes;
    private readonly long? _warningFreeBytes;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="path">A path on the drive to check (e.g. <c>"/"</c> or <c>"C:\\"</c>).</param>
    /// <param name="minimumFreeBytes">Below this many free bytes the check fails.</param>
    /// <param name="warningFreeBytes">Optional soft threshold: below this (but at/above the minimum) the check warns.</param>
    public DiskHealthCheck(string path, long minimumFreeBytes, long? warningFreeBytes = null)
    {
        _path = path;
        _minimumFreeBytes = minimumFreeBytes;
        _warningFreeBytes = warningFreeBytes;
    }

    /// <inheritdoc />
    public string Type => "Disk";

    /// <inheritdoc />
    public Task<IHealthCheckResult> ExecuteAsync()
    {
        try
        {
            var drive = new DriveInfo(_path);
            var free = drive.AvailableFreeSpace;

            var dependencies = new[] { new HealthCheckDependency("Disk", drive.Name) };
            var data = new Dictionary<string, object>
            {
                { "Drive", drive.Name },
                { "FreeBytes", free },
                { "TotalBytes", drive.TotalSize },
                { "MinimumFreeBytes", _minimumFreeBytes },
            };

            if (free < _minimumFreeBytes)
            {
                return Task.FromResult(HealthCheckResult.CreateInstance(false, Type, data, dependencies));
            }

            if (_warningFreeBytes.HasValue && free < _warningFreeBytes.Value)
            {
                return Task.FromResult(HealthCheckResult.CreateWarning(Type, data, dependencies));
            }

            return Task.FromResult(HealthCheckResult.CreateInstance(true, Type, data, dependencies));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object> { { "Path", _path }, { "Error", ex.GetType().Name } }));
        }
    }
}
