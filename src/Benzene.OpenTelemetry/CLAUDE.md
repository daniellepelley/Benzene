# Benzene.OpenTelemetry

## What this package does
Glue that wires `Benzene.Diagnostics`'s `ActivitySource`/`Meter` (both named `"Benzene"`) into an
OpenTelemetry `TracerProviderBuilder`/`MeterProviderBuilder`, so the `Activity` spans and metric
instruments Benzene already produces get exported to a real backend. This package registers no
Benzene DI services and replaces no `IProcessTimerFactory` — it only extends OTel's own builder
types.

## Key types/interfaces
- `AddBenzeneInstrumentation(this TracerProviderBuilder)` - calls `AddSource("Benzene")`
- `AddBenzeneInstrumentation(this MeterProviderBuilder)` - calls `AddMeter("Benzene")`

## When to use this package
- Call `AddBenzeneInstrumentation()` when building your `TracerProviderBuilder`/`MeterProviderBuilder`
  (via `Sdk.CreateTracerProviderBuilder()`/`Sdk.CreateMeterProviderBuilder()` or
  `OpenTelemetry.Extensions.Hosting`'s `AddOpenTelemetry()`) to export Benzene's spans/metrics
  alongside your own instrumentation, to whatever exporter you configure (OTLP, console, etc.)

## Dependencies on other Benzene packages
- **Benzene.Diagnostics** - source of the `ActivitySource`/`Meter` this package registers
- **OpenTelemetry.Api** - `TracerProviderBuilder`/`MeterProviderBuilder` and `AddSource`/`AddMeter`

## Important conventions
- No Benzene DI registration is required or provided by this package — `AddDiagnostics()` (from
  `Benzene.Diagnostics`) is what produces the spans/metrics; this package only exports them
- Without a configured exporter, `ActivitySource.StartActivity`/metric recording remain effectively
  free no-ops, same as without this package at all
