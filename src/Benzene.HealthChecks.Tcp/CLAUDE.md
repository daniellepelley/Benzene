# Benzene.HealthChecks.Tcp

## What this package does
A single `IHealthCheck` (`TcpHealthCheck`) that verifies L4 reachability by opening a TCP connection to
a host and port. The lowest-common-denominator "is it reachable" check for a dependency with no
first-class Benzene client (a database port, SMTP, a custom/non-HTTP service). Mirrors
`Benzene.HealthChecks.Http` (client-free, references only `Benzene.HealthChecks.Core`).

## Key types
- `TcpHealthCheck` - `TcpClient.ConnectAsync(host, port, token)`; healthy on connect, unhealthy on any
  socket error (reports the exception **type name**, never its message). Observes the ambient
  cancellation token via `ICancellationTokenAccessor` (optional). `Type => "Tcp"`; `Data` = Host/Port
  (+ Error on failure); `Dependencies` = one `HealthCheckDependency("Tcp", "host:port")`.
- `TcpHealthCheckFactory` - builds the check for a fixed host/port, resolving the cancellation accessor.
- `Extensions.AddTcpPing(builder, host, port)` - registration helper on `IHealthCheckBuilder`.

## Conventions
- No independent timeout - relies on the aggregator's `TimeOutHealthCheck` wrapper (and the ambient
  cancellation token).
- Only a completed connect is healthy; a refused/failed connect is a failed result, not a thrown
  exception.
