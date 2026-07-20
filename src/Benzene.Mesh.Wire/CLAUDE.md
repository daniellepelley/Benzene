# Benzene.Mesh.Wire

## What this package does
The spec-conformant mesh **wire layer** of `docs/specification/mesh.md` - what makes a .NET
Benzene service a citizen of a cross-language mesh fleet: the derived ServiceDescriptor (§2,
topics + CLR-derived payload schemas + `descriptorHash`), the reserved `mesh` topic middleware
(§1), the trace middleware emitting semantic TraceEvents (§3), W3C traceparent join and
`MeshSpan.Current` propagation, and the batching HTTP trace exporter with the §4 sender rules.
Verified against the language-neutral fixtures by `test/Benzene.Conformance.Test`'s
`MeshDescriptorConformanceTest`/`MeshTraceConformanceTest`, and cross-language against the Go
reference collector (see `work/service-mesh-roadmap-1.0.md`'s 2026-07-16 updates).

Distinct from the pre-existing `Benzene.Mesh.Contracts`/`Aggregator`/`Reporting` visibility
pipeline (spec §9 maps the two): this package is the *wire contract*; the aggregator remains the
pull-based collector idiom and can adopt these shapes as ingest sources (roadmap, still open).

## Key types/interfaces
- `MeshServiceDescriptor`/`MeshTopicDescriptor`/`MeshPlacement` + `MeshDescriptorFactory.Create(
  IMessageHandlerDefinitionLookUp?, MeshServiceInfo)` - the §2 descriptor derived from the live
  handler registry (topics sorted by id then version). A null lookup degrades to a topic-less
  descriptor with `degraded: ["registry"]`, per §6 - never an error.
- `MeshSchemaGenerator.Derive(Type)` - the §2.1 CLR→JSON Schema mapping (startup-only
  reflection). "required" = properties the marshaler always emits: nullable-annotated (NRT or
  `Nullable<T>`) and ignore-when-null properties are optional. Recursion cut with `{}`.
- `MeshDescriptorHashing` - §2.2: SHA-256 over canonical camelCase JSON with `instanceId`/
  `degraded`/`profile`/`descriptorHash` blanked. NOT the same thing as
  `Benzene.Mesh.Contracts.MeshHashing` (HMAC over raw OpenAPI text for the aggregator's artifact
  drift) - do not merge them.
- `MeshServiceDescriptor.Profile` (`MeshProfile`: `Name` + `Missing`) - the optional §2 `profile`
  field, a named conformance-profile self-assessment (e.g. `Benzene.CloudService`'s Cloud Service
  Profile report). Self-description like `Degraded`, so excluded from the hash above; this
  package only carries the shape; `Benzene.CloudService` is what stamps it.
- `Extensions.UseMeshDescriptor(descriptor, aliases...)` - reserved-topic interception, same
  pattern as `UseHealthCheck`. `Extensions.UseMeshTrace(info, exporter, statusReader)` - wire it
  **outermost**; per-invocation TraceEvent with traceparent join, `MeshSpan.Current` set across
  `next()`, status read back via `IMeshStatusReader<TContext>` (BenzeneMessage reader ships here;
  other transports add their own, following the `IMessageGetter<TContext>` mapper idiom).
- `HttpMeshTraceExporter` - bounded channel (DropWrite), batches to a collector's envelope
  endpoint as `mesh:traces`. Lossy by design in every failure mode (§4); `DisposeAsync` flushes
  the tail and is idempotent. Implements **both** `IAsyncDisposable` and `IDisposable`: MS DI's
  synchronous `ServiceProvider.Dispose()` throws on an async-only-disposable, so the sync `Dispose()`
  bridges to `DisposeAsync` with a bounded wait (an unreachable collector must not hang shutdown -
  lossy by design, so an abandoned overlong tail-flush is fine). See
  `test/Benzene.Core.Test/CloudService/MeshDisposalTest.cs`.
- `MeshTopics` / `MeshTraceEvent` / `MeshTraceBatch` / `MeshHeartbeat` - the wire shapes.
  `MeshHeartbeat.Health` reuses `HealthChecks.Core.HealthCheckResponse` as-is.

## Important conventions
- **The spec wins.** These shapes are pinned by `docs/specification/conformance/mesh-*.json`;
  changing them means changing the spec + fixtures first (and the Go reference implementation
  alongside).
- **Degradation is normative (spec §6)**: no mesh feed may ever fail, slow, or block the
  invocation it observes. The trace middleware swallows exporter exceptions; the exporter drops
  on full buffer and failed sends; a missing status reader yields an empty status.
- Wire JSON is camelCase with nulls omitted - always serialize through `MeshJson.Options` so the
  descriptor hash and the wire bytes can't drift apart.

## Dependencies on other Benzene packages
Abstractions.MessageHandlers (definitions lookup, mappers), Core.MessageHandlers /
Core.Middleware (interception idiom), Core.Messages (BenzeneMessage status reader),
HealthChecks.Core (heartbeat health shape), Results.

## Tests
- `test/Benzene.Conformance.Test/MeshDescriptorConformanceTest.cs` /
  `MeshTraceConformanceTest.cs` - the language-neutral fixture-driven conformance suite (wire
  shapes, hash invariants).
- `test/Benzene.Mesh.Test/Wire/MeshSchemaGeneratorTest.cs` - direct unit coverage of every
  `MeshSchemaGenerator.Derive` branch: each primitive/date/byte-array mapping, the unconstrained
  `{}` cases (object/JsonElement/enum), `Nullable<T>`'s added `"null"` type, dictionary/enumerable
  mapping, `JsonPropertyName`/`JsonIgnore` (`Always` and `WhenWritingDefault`) handling, the
  nullable-annotated-property → optional "required" rule, lexicographic property ordering, and
  cycle-cutting on a self-referencing type. None of this had a test before (the conformance suite
  only exercises it indirectly through the canonical conformance handlers' payload shapes).
- `test/Benzene.Mesh.Test/Wire/ExtensionsTest.cs` - end-to-end pipeline tests for
  `UseMeshDescriptor`/`UseMeshTrace`, built on a real `BenzeneMessageContext`/
  `BenzeneMessageApplication` pipeline (mirrors `HealthCheckPipelineTest`'s pattern) since
  `Benzene.Mesh.Test` didn't previously reference this package or `Core.MessageHandlers`/
  `Core.Messages`/`Microsoft.Dependencies` (now added as `ProjectReference`s). Covers: descriptor
  topic + alias matching vs. fall-through, `UseMeshTrace`'s null-exporter pass-through, a
  successful export's topic/service/status/duration/ids, W3C traceparent join vs. missing/
  malformed-header fresh-trace fallback, a throwing exporter not affecting the response,
  no-status-reader → empty status, the `x-correlation-id` header capture, and `MeshSpan.Current`
  being set during `next()` and restored afterward. Setting up these tests requires
  `.AddContextItems()` explicitly (not part of `AddBenzeneMessage()` - only `AddMessageHandlers()`
  pulls it in) since none of these tests route through `.UseMessageHandlers()`.
