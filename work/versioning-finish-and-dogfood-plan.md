# Finishing message versioning + dogfooding it in the mesh examples

## Context
Message versioning in Benzene is **~80% built** (see `docs/specification/versioning.md`, whose top
banner is stale — it says "not implemented" but the code contradicts it). Already shipped & tested:

- Topic = `(Id, Version:string)`; `[Message(topic, version)]` handler dispatch via `IVersionSelector`.
- Full upcast/downcast engine — `Benzene.Core.Versioning` (`ICaster`, `SchemaCaster`, `SchemaCasters`,
  BFS chain composition, `CasterFactory`), request-side `CastingRequestMapper` + response-side
  `CastingResponsePayloadMapper`, one-call opt-in `UsePayloadVersionCasting<TContext>()`.
- **Inbound** version read per transport — HTTP route param `version` (`/v{version}/…`) with header
  fallback; all other transports read `benzene-version`/`version`/`x-version` via `HeaderMessageVersionGetter`.
- Mesh carries version end-to-end — `MeshTopicEntry` keyed by `(topic, version)`, UI has a "Version"
  column + per-version drill-in; spec emits `requests[].version`/`events[].version`.
- End-to-end test: `PayloadVersionCastingPipelineTest` (V1 producer → upcast → single V2 handler).

## Gaps to close (this work)
1. **Outbound version-write ergonomics.** The `benzene-version` header already flows end-to-end, but no
   first-class API sets it. Add a shared header constant + `version`-aware overloads on
   `IBenzeneMessageSender` / `IBenzeneMessageClient` so a producer can `SendAsync(topic, req, version: "2")`.
2. **HTTP `/v{version}` route policy** — the one genuinely new framework piece. Opt-in; when on, every
   `[HttpEndpoint]` route is also exposed at `/v{version}/<path>` (default-latest bare route kept,
   versioned route added); absent when off; per-endpoint override. Decorate `IHttpEndpointFinder` (the
   single chokepoint both `RouteFinder` and the spec builder consume).
3. **Mesh cross-version compatibility.** Today each `(topic,version)` is an island. Compute, per topic id,
   produced-versions vs consumed-versions, and surface "producer emits vN, nobody consumes vN" (and the
   reverse). New signal on `MeshTopicEntry`/`MeshTopicStatus`, computed in `MeshAggregator.BuildCatalog`,
   rendered on the topic drill-in + topics table.
4. **Dogfood** in `examples/Mesh` (easy `./run.sh` live demo) **and** `examples/K8sMesh` (the HTTP
   envelope chain): a topic with v1 & v2, a single v2 handler, a registered v1→v2 upcaster,
   `UsePayloadVersionCasting`, a producer sending both versions, `/v1`/`/v2` HTTP routes, versions +
   compatibility visible in the Mesh UI.
5. **Docs** — fix the stale banner; add a `docs/cookbooks/` versioning entry.

## Version-token convention (decided)
The wire token is the same everywhere: HTTP route `/v{version}` captures the token, and the header
`benzene-version` carries the same token, and the caster schema-name is the same token. Demo uses
`"1"`/`"2"` (route `/v1`,`/v2`; header `benzene-version: 1|2`; schemas `"1"`/`"2"`; mesh version `1`/`2`).

## Slices (each builds + tests + commits)
1. Version header constant (`Benzene.Abstractions.Messages`) + outbound overloads (`Benzene.Clients`,
   `Benzene.Client.Http`). Unit tests.
2. `Benzene.Http` `/v{version}` route policy: `HttpVersioningOptions` + `VersionedHttpEndpointFinder`
   decorator + `AddHttpVersioning()` opt-in. Unit tests (routes doubled; bare→latest still matches).
3. Mesh cross-version compatibility: `MeshTopicEntry` field / `MeshTopicStatus` extension +
   `MeshAggregator` compute + `mesh-ui.html` render. Aggregator tests.
4. Dogfood `examples/Mesh`: v1/v2 `payments:get` (or a new topic), upcaster, producer, `/v1`/`/v2`.
5. Dogfood `examples/K8sMesh`: v1/v2 over the orders→payments HTTP envelope chain.
6. Docs: banner fix + cookbook.

Verification: `dotnet build Benzene.sln` + targeted tests per slice; run `examples/Mesh/run.sh`-style
local exercise where feasible (as done for the HTTP client work).

## Status: COMPLETE (all slices shipped)
1. ✅ Outbound version-send (`MessageVersionHeaders.Default`, `SendAsync/SendMessageAsync(..., version)`, `WithVersion`).
2. ✅ HTTP `/v{version}` route policy (`HttpVersioningOptions`, `VersionedHttpEndpointFinder`, `AddHttpVersioning()`).
3. ✅ Mesh cross-version compatibility (`MeshTopicVersionCompatibility`, aggregator compute, Mesh UI panel).
4. ✅ `examples/Mesh` dogfood — **verified live**: `/v1`|`/v2` routes + response downcast; mesh skew in `topics.json`.
5. ✅ `examples/K8sMesh` dogfood — **verified live**: v1 request upcast to a single v2 handler over the envelope.
6. ✅ Docs — stale banner fixed in `docs/specification/versioning.md`.

Tests: 59 core (versioning/http/clients) + 38 mesh aggregator pass; full `Benzene.sln` builds clean.
