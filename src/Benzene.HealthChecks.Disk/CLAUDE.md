# Benzene.HealthChecks.Disk

## What this package does
A single `IHealthCheck` (`DiskHealthCheck`) - a host self-check on free disk space for the drive
containing a given path. BCL-only (`System.IO.DriveInfo`), references only `Benzene.HealthChecks.Core`.

## Key types
- `DiskHealthCheck` - `Failed` below a hard `minimumFreeBytes`, an optional `Warning` below a soft
  `warningFreeBytes` (degraded-but-not-fatal: `Warning` does not flip aggregate `IsHealthy`), otherwise
  healthy. `Type => "Disk"`; `Data` = Drive/FreeBytes/TotalBytes/MinimumFreeBytes; `Dependencies` = one
  `HealthCheckDependency("Disk", drive.Name)`.
- `Extensions.AddDiskSpaceCheck(builder, path, minimumFreeBytes, warningFreeBytes?)` - registration helper.

## Conventions
- The two-threshold (warn/fail) shape is the canonical use of `HealthCheckResult.CreateWarning` - a soft
  threshold surfaces low disk before it becomes fatal without failing the whole aggregate.
