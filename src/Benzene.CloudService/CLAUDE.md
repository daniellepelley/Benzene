# Benzene.CloudService

## What this package does
The batteries-included setup for a **Benzene Cloud Service** — the code-level counterpart of
`docs/specification/cloud-service-profile.md`. One call, `UseBenzeneCloudService("name", ...)`,
on a hosted HTTP pipeline wires every operational surface the profile requires (R1–R8) at the
default service standard paths: the `/benzene/invoke` envelope endpoint, `/benzene/spec`,
`/benzene/health` + reserved `healthcheck` topic, the reserved `mesh` descriptor topic, the trace
feed, and collector registration + heartbeats. It is deliberately **syntactic sugar** over the
same pipeline builders Benzene Core setup uses — no new capability lives here, only the profile's
steers pre-wired in the right order. Manual composition of the underlying `Use*` calls (Benzene
Core setup) remains the full-control path and this package must never be required for it.

## Key types
- `Extensions.UseBenzeneCloudService<TContext>(serviceName, configure?)` — the entry point;
  `TContext : IHttpContext`, so it works on any HTTP transport (ASP.NET, API Gateway, self-host).
  Terminal: it ends with the message router; app HTTP middleware goes *before* the call.
- `ICloudServiceBuilder` / `CloudServiceBuilder` — the configuration surface. Every setting has a
  profile-conformant default; overrides are always honored and honestly reflected in the report
  (`WithoutMesh()`, relocated paths → flagged, never refused).
- `CloudServiceProfileReport` / `CloudServiceRequirement` — the wiring-time self-assessment
  against R1–R8. Registered in DI, stamped on the descriptor as mesh.md §2's `profile` field
  (`MeshProfile`, lives in `Benzene.Mesh.Wire`, excluded from the descriptor hash like
  `degraded`). It reports *provisioning*, not runtime state — runtime degradation is not a
  conformance failure (profile §4).
- `CloudServiceDescriptorSource` — single descriptor source for the reserved-topic middleware and
  the announcer. Eager when handler types are explicit (`WithHandlers`), else derived lazily from
  the container's `IMessageHandlerDefinitionLookUp` on first use.
- `MeshAnnouncer` — the outbound mesh feeds (mesh.md §4–§5): register-with-retry then heartbeats
  (health via `HealthCheckProcessor`). Started at wire-up on the eager path; by the first
  invocation (typically a platform health probe) on the lazy path.
- `CloudServicePaths` — the `/benzene/*` default-standard path constants.

## Important conventions
- **Spec §6 degradation is law here too**: the announcer and trace exporter swallow every
  failure; no mesh feed may ever fail, slow, or block an invocation.
- **Two pipelines, one registry.** The envelope pipeline (`/benzene/invoke`) and the outer HTTP
  pipeline both route through the same handler definitions; health checks are wired on both, the
  mesh trace + descriptor only on the envelope pipeline (service-to-service mesh traffic speaks
  the envelope — see the Mesh examples).
- Pipeline order inside the envelope surface: announce-start → trace (outermost, sees everything)
  → health interception → descriptor interception → app middleware (`WithMiddleware`) → router.
- Adding a profile requirement means updating `CloudServiceProfileReport.Evaluate`, the profile
  spec doc, and this wiring together — the report must never claim what the builder didn't wire.

## Dependencies on other Benzene packages
Http (envelope endpoint, routes, `IHttpContext`), HealthChecks + HealthChecks.Core,
Schema.OpenApi (spec handler), Mesh.Wire (descriptor/trace/heartbeat wire layer),
Core.MessageHandlers / Core.Messages / Core.Middleware.
