# Reserved-Topic `benzene:` Prefix — migration plan

**Status:** PLAN — executing. Task #29. Applies the accepted ruling in
`work/benzene-naming-principle.md`; the principle itself is settled and not reopened here.
**Last Updated:** 2026-07-25
**Purpose:** Apply the `benzene:` prefix to every reserved topic id, without missing occurrences —
the failure mode is a silent one (a literal left behind still compiles, and only fails at runtime or
in a fixture nobody re-reads).

---

## 1. The one decision left: clean break, no transition period

**Ruling: clean break at 1.0. Old ids are not accepted.**

- `version.txt` is `0.0.2`, `git tag` is empty, everything published is alpha. There is no
  installed base with a contract to honour.
- A dual-accept shim is permanent complexity — every reader, every conformance fixture, and every
  port would carry "or the legacy id" forever, to spare an alpha population that does not exist.
- The one live consumer is our own AwsMesh demo, which we redeploy (and now have a teardown for).

## 2. The rename map

**Reserved utility topics** (`Benzene.Schema.OpenApi/ReservedTopics.cs` + the `Constants.cs` files):

| Old | New |
|---|---|
| `spec` | `benzene:spec` |
| `test-payloads` | `benzene:test-payloads` |
| `healthcheck` | `benzene:healthcheck` |
| `liveness` | `benzene:liveness` |
| `readiness` | `benzene:readiness` |
| `mesh` | `benzene:mesh` |
| `invoke` | `benzene:invoke` |
| `report` | `benzene:report` |
| `ping` (transport health probe) | `benzene:ping` |

**Mesh wire topics** — `mesh:*` → `benzene:mesh:*`:

`benzene:mesh:register`, `benzene:mesh:heartbeat`, `benzene:mesh:traces`, `benzene:mesh:issues`,
`benzene:mesh:report`, `benzene:mesh:aggregate`, `benzene:mesh:dispatch`, `benzene:mesh:topology`,
`benzene:mesh:annotations:add`, and `benzene:mesh:query:{fleet,service,topic,trace,correlation}`.

**Explicitly NOT renamed:**
- **HTTP paths** (`/benzene/spec`, `/benzene/health`, `/benzene/invoke`) — already marked, and they
  are *separate constants* (`CloudServicePaths`/`CloudServiceProbePaths`), not derived from topic
  ids. Verified before starting; this was the main corruption risk (`/benzene/benzene:spec`).
- **Envelope fields**, **status vocabulary**, **borrowed headers** — out of scope by the principle.
- Note the resulting asymmetry: path `/benzene/health` beside topic `benzene:healthcheck`. Two
  different surfaces that already spelled it differently; the principle governs *marking*, not
  spelling. Left as-is deliberately.

## 3. Execution order (centralise → change → sweep → verify)

The order matters: renaming constants first and *then* hunting literals means the compiler finds
most of the work for us, and the residue is a much smaller manual search.

1. **Centralise.** Any `src/` literal that should be a constant becomes one. No behaviour change;
   this is what shrinks the blind-search surface.
2. **Change the constants** (`Benzene.Schema.OpenApi/Constants.cs`,
   `Benzene.HealthChecks/Constants.cs`, `Benzene.Aws.Lambda.ApiGateway/Constants.cs`,
   `ReservedTopics.DefaultIds`) and the mesh topic literals in `Benzene.Mesh.*`.
3. **Sweep the residue** — remaining `src/` literals, then `test/`, `examples/`, `templates/`.
4. **Conformance fixtures** — `mesh-collector-cases.json`, `mesh-issue-cases.json`, and any
   envelope case naming a reserved topic.
5. **Spec docs** — `mesh.md` §1 (the reserved-topic requirement), `cloud-service-profile.md`,
   `wire-contracts.md`, `core-concepts.md`, `design-principles.md`, `README.md`.
6. **Mesh UI** — `isUtilityTraffic()` collapses from a hardcoded literal list to a single
   `benzene:` prefix test (plus the catalog-`reserved` flag). This is the payoff the UI code's own
   comment predicted.
7. **Verify** — full build; `Benzene.Core.Test`, `Benzene.Mesh.Test`, `Benzene.Conformance.Test`;
   the mesh-UI smoke harness; then a grep audit for any surviving bare literal.

## 4. Verification that the sweep was complete

The silent-failure risk is a literal left behind. After the change, a bare-word audit must come
back clean:

- No `"mesh:` in `src`/`test`/`examples` (only `"benzene:mesh:`).
- Every remaining bare `"spec"`/`"healthcheck"`/`"invoke"`/`"report"`/`"ping"`/`"mesh"` in a
  topic-shaped position is accounted for — many legitimately remain (HTTP path segments, gRPC
  service names, CLI command names, health-check *registration* names like `benzene-liveness`,
  JSON property names). Each survivor is checked, not assumed.

## 5. Blast radius (measured before starting)

- `src`: 41 reserved-topic literal occurrences + 14 `mesh:*` literals; the topic constants are
  already centralised in 6 constants across 3 files, which is what makes this tractable.
- Conformance fixtures: 2 files.
- Spec: 6 documents.
- Mesh UI: 1 filter function (simplified, not complicated, by this change).
- Examples/templates: AwsMesh + the mesh examples declare and consume these topics.
