# Kubernetes Health Checks

`Benzene.HealthChecks` provides purpose-built liveness/readiness convenience methods on top of the
general-purpose [health checks](health-checks.md) support, matching [Kubernetes' own liveness/readiness/
startup probe model](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/).
This page covers the semantics, how to wire them up per transport, and example probe configuration.
Read [Health Checks](health-checks.md) first if you haven't already — this page assumes you know
`IHealthCheck`/`IHealthCheckBuilder` and doesn't repeat that material.

## Liveness vs. readiness

Kubernetes' guidance is specific about what belongs in each, and Benzene doesn't second-guess it:

- **Liveness** answers "is this process itself still working?" A liveness probe failure causes
  Kubernetes to **restart the pod**. Keep liveness checks cheap and local — verify the process can
  still do work at all (e.g. an internal loop isn't stuck), not that every external dependency is
  reachable. Checking a database or downstream service in a liveness probe is a well-known
  anti-pattern: a flaky dependency would cause Kubernetes to restart pods that are actually fine,
  potentially triggering a restart storm across your whole fleet in response to one struggling
  downstream service.
- **Readiness** answers "is this instance ready to receive traffic right now?" A readiness probe
  failure only **removes the pod from the Service's endpoint list** — no restart, traffic just stops
  routing to it until it passes again. This is where external-dependency checks belong (database
  connectivity, a required downstream API) — the correct response to "the database is down" is
  "stop sending this instance traffic," not "restart the process," since restarting won't fix a
  database outage.

`Benzene.HealthChecks.EntityFramework`'s `DatabaseConnectionHealthCheck`/`DatabaseHealthCheck` and
`Benzene.HealthChecks.Http`'s `HttpPingHealthCheck` are all external-dependency checks — register
them under readiness, not liveness.

## Topic-based wiring (every transport): `UseLivenessCheck` / `UseReadinessCheck`

```csharp
using Benzene.HealthChecks;
using Benzene.HealthChecks.EntityFramework;

app.UseLivenessCheck(x => x
    .AddHealthCheck<ProcessResponsiveCheck>()); // your own check, cheap, no external I/O

app.UseReadinessCheck(x => x
    .AddHealthCheckFactory(new DatabaseHealthCheckFactory<MyDbContext>("20250101000000_Latest"))
    .AddHttpPing("https://downstream-service/health"));
```

Each responds only to its own topic (`Constants.DefaultLivenessTopic` = `"liveness"`,
`Constants.DefaultReadinessTopic` = `"readiness"`) — unlike `UseHealthCheck()`, neither also matches
`Constants.DefaultHealthCheckTopic`. This is deliberate: if both did, whichever was registered first
in the pipeline would silently swallow every request for the shared `"healthcheck"` topic, and the
other would never run for it.

## HTTP-path wiring: `Benzene.SelfHost.Http`, `Benzene.Aws.Lambda.ApiGateway`

These two packages deal with raw HTTP requests directly, so they expose method+path matching instead
of (in addition to) topic matching — defaulting to the conventional Kubernetes probe paths:

```csharp
app.UseLivenessCheck(x => x.AddHealthCheck<ProcessResponsiveCheck>());              // GET /livez
app.UseReadinessCheck(x => x.AddHealthCheckFactory(dbHealthCheckFactory));           // GET /readyz

// override the path if your ingress/probe config expects something else
app.UseLivenessCheck("/healthz/live", x => x.AddHealthCheck<ProcessResponsiveCheck>());
```

### ASP.NET Core

ASP.NET Core doesn't have an equivalent HTTP-path overload (an ASP.NET Core request's topic is
already resolved via routing before Benzene's middleware runs — see [Health Checks](health-checks.md#http-method--path-based-raw-http-transports)
for why) — use the topic-based `UseLivenessCheck`/`UseReadinessCheck` there, and register an
`IHttpEndpointDefinition` mapping the conventional paths to the liveness/readiness topics so an
incoming `GET /livez`/`GET /readyz` actually resolves to the right topic:

```csharp
using Benzene.HealthChecks;
using Benzene.Http.Routing;

public class MyStartUp : BenzeneStartUp
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers()
            .AddSingleton<IHttpEndpointDefinition>(_ =>
                new HttpEndpointDefinition("GET", "/livez", Constants.DefaultLivenessTopic))
            .AddSingleton<IHttpEndpointDefinition>(_ =>
                new HttpEndpointDefinition("GET", "/readyz", Constants.DefaultReadinessTopic)));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UseHttp(http => http
            .UseLivenessCheck(x => x.AddHealthCheck<ProcessResponsiveCheck>())
            .UseReadinessCheck(x => x.AddHealthCheckFactory(new DatabaseHealthCheckFactory<MyDbContext>("...")))
            .UseMessageHandlers()); // your regular handlers, if any
}
```

No `IMessageHandlerDefinition` registration is needed for the health check routes — `UseLivenessCheck`/
`UseReadinessCheck` are raw middleware that intercept the request directly by topic, before
`.UseMessageHandlers()`'s own handler-resolution would otherwise run.

## Example Kubernetes manifest

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: my-benzene-service
spec:
  template:
    spec:
      containers:
        - name: my-benzene-service
          image: my-registry/my-benzene-service:latest
          ports:
            - containerPort: 8080
          livenessProbe:
            httpGet:
              path: /livez
              port: 8080
            initialDelaySeconds: 5
            periodSeconds: 10
          readinessProbe:
            httpGet:
              path: /readyz
              port: 8080
            initialDelaySeconds: 5
            periodSeconds: 10
```

Both probes work correctly against Benzene's response because the HTTP status code — not just the
JSON body — reflects health: `200` when healthy, `503 Service Unavailable` when not (see
[HTTP status codes](health-checks.md#http-status-codes)). Kubernetes' `httpGet` probe type only inspects
the status code (2xx-399 is a pass, anything else is a failure), so this matters — a health check
that returned `200` unconditionally, with the real status only visible in the response body, would
never actually fail a Kubernetes probe.

If you'd rather probe over gRPC, see [Health Checks — gRPC (grpc.health.v1)](health-checks.md#grpc-grpchealthv1)
for `Benzene.Grpc.AspNet`'s standard-protocol bridge, which Kubernetes' native gRPC probe type
(`grpc.livenessProbe`/`readinessProbe.grpc`) can query directly.

### gRPC liveness/readiness split

By default `Benzene.Grpc.AspNet`'s bridge aggregates *every* registered `IHealthCheck` into the single
overall `grpc.health.v1` service (empty service name). That's fine for a readiness probe or a plain
"is it up" check — but a **liveness** probe pointed at it would run your external-dependency (readiness)
checks too, exactly the restart-on-flaky-dependency anti-pattern the
[liveness/readiness split](#liveness-vs-readiness) exists to avoid.

To get the same split over gRPC that HTTP `UseLivenessCheck`/`UseReadinessCheck` give you, set
`LivenessCheckTypes`/`ReadinessCheckTypes` on `BenzeneGrpcOptions` — lists of the check `Type`s that
belong in each. Benzene then publishes named grpc.health.v1 services `"liveness"` and `"readiness"`
alongside the overall `""` service, each reporting only its own subset:

```csharp
services.AddBenzeneGrpc(o =>
{
    o.EnableHealthChecks = true;
    o.LivenessCheckTypes = new[] { "ProcessResponsive" };          // cheap/local checks only
    o.ReadinessCheckTypes = new[] { "Database", "HttpPing" };       // external-dependency checks
});
```

Kubernetes' native gRPC probe type can then target the right service name:

```yaml
livenessProbe:
  grpc:
    port: 8080
    service: liveness
readinessProbe:
  grpc:
    port: 8080
    service: readiness
```

When neither is set, behaviour is unchanged: a single aggregate `"benzene"` check on the overall `""`
service.

## What this does not cover

- **Startup probes.** Kubernetes' third probe type (for slow-starting containers) has no dedicated
  Benzene wiring — reuse `UseReadinessCheck` (or a separate topic/path of your own) for it; there's
  nothing startup-probe-specific in Benzene today.
- **Graceful shutdown coordination.** Flipping readiness to unhealthy during pod termination (so
  Kubernetes stops routing new traffic before `SIGTERM` is sent) is an application-level concern —
  Benzene doesn't automatically wire your readiness check to the host's shutdown lifecycle.

## See also

- [Health Checks](health-checks.md) — the general-purpose health check system this builds on
- [Privacy & Data Handling](privacy-and-data-handling.md) — what health check diagnostic data is safe
  to expose to whatever can reach these endpoints
- [Monitoring & Diagnostics](monitoring.md) — tracing, logging, and metrics for the rest of your pipeline
