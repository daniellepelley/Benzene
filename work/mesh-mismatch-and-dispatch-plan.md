# Mismatch made visible, and dispatch made real — implementation plan

**2026-08-20.** Two problems from the deployed AWS mesh, researched by four parallel agents (view
design, aggregator data, dispatch wiring, security envelope) and synthesized here into work packages
an implementing agent can execute directly. Companion docs: `mesh-ui-aims.md` (rules cited as R*),
`mesh-environments-and-access.md` (E*).

The two problems are one species: **the UI knows something the reader cannot see or do.** It knows
services disagree on a schema and will not show where; it renders a Send button that cannot send.

---

## Part A — the schema mismatch, shown

### A0 · RULING: publish raw per-service declarations, not computed diffs

The data agent recommended computed per-consumer diffs against a named baseline (small, feeds the
existing annotation machinery, matches vision §5.5's "emit the differing paths"). The design agent's
chosen view — one merged **union tree**, every field any service declares, variant lines only where
they disagree — is a pure walk over **raw declarations** and cannot be built from diffs.

Ruled: **raw declarations win**, on four grounds:

1. The union tree is the design, and diffs cannot feed it. Deciding data shape by renderer is
   correct here — the artifact exists for this view.
2. Size stays bounded: `declaredSchemas` is published **only when `schemaMismatch` is true**, which
   is rare by construction, and N consumers is small in practice.
3. It closes an honesty hole rather than documenting one. The .NET comparer classifies only
   `type/format/properties/required/items`, so a mismatch tripped by a `pattern` or `maxLength`
   would publish an *empty* diff ("differs, cannot say where"). The UI's facet renderer already
   shows those keywords, so a union walk over raw schemas can show the actual difference.
4. R4: choosing a diff baseline crowns one consumer as the reference. Raw declarations have no
   baseline at all — symmetry by construction, not by discipline.

The diff-based `mismatchDetail` design is NOT built. If a future surface needs pre-classified paths
(a CI gate, a port without a schema walker), it can be added additively then.

### WP-A1 · benzene-dotnet: publish `declaredSchemas` *(agent-ready)*

1. `MeshAggregator.cs` — `TopicAggregate.ConsumerSchemas` becomes
   `List<(string Service, JsonObject? Request, JsonObject? Response)>`; pass `entries[i].Name` at
   the ~line 446 append. This kills a fragile parallel-list coupling (service attribution currently
   exists only positionally against `Consumers`) and is worth doing even alone.
2. New contract `src/Benzene.Mesh.Contracts/MeshDeclaredSchema.cs`:
   `{ string Service; string Role /* "consumer" | "producer" */; JsonObject? RequestSchema;
   JsonObject? ResponseSchema; JsonObject? MessageSchema; }` — loose strings per the repo's wire
   convention. `MeshTopicEntry` gains defaulted ctor param + property
   `MeshDeclaredSchema[]? DeclaredSchemas`; doc-comment: null = this build does not publish it
   (≠ nothing differs); non-null **only when `SchemaMismatch` is true**; never on reserved topics.
3. `BuildTopicEntry`: when mismatch is true, emit one entry per consumer with a non-null schema on
   either side (role `consumer`), and one per producer whose `MessageSchema` the aggregate holds
   (role `producer` — design agent's PO question 2, taken: yes, the union walk is role-agnostic).
   Consumers that declared nothing are absent = no signal, never agreement.
4. Carry the field through BOTH entry rebuilds — `WithCompatibility` and `WithChanges` — the
   documented field-dropping defect class caught once already this week.
5. Tests (style of the existing mismatch trio and `RunOnceAsync_SecondRun_ClassifiesTheDriftDownToTheField`):
   mismatch publishes per-service schemas keyed correctly; no mismatch → null; response-only
   mismatch attributes sides independently; reserved topics never publish; a mismatch topic that
   also drifts run-over-run keeps `DeclaredSchemas` after `WithChanges` (the rebuild guard); the
   entry's representative `RequestSchema` byte-equals the first declarer's copy in
   `DeclaredSchemas`.
6. benzene-ui sample artifact: `contracts/artifacts/topics.json` currently has
   `schemaMismatch: false` everywhere and single consumers — the demo narrative claims a
   `shipping:book` mismatch the data never shows (a known fixture defect, vision §5.9). Make the
   story true: second consumer on `shipping:book`, `schemaMismatch: true`, `declaredSchemas` with a
   presence difference, a type conflict, and a required difference; add the missing request schemas.
   Then `npm run generate:contracts` — the generated-type diff IS the contract change.

### WP-A2 · benzene-ui: the union tree *(agent-ready; full spec in the design report)*

Design (mesh-ux-designer, accepted in full):

- **One merged union tree** in the Contract card. Agreeing fields render exactly as today's
  SchemaTree rows — silent. A divergent field gets one variant line per distinct declaration:
  `string — orders-api, billing-api` / `not declared — shipping-api` / `string, optional — billing-api`.
  Reading cost is proportional to the disagreement, not the estate.
- **Symmetric vocabulary (R4)**: marker is `differs`, never missing/extra/wrong; no variant ordered
  or styled as the reference (groups largest-first, ties alphabetical — a sort, not a ruling).
  Marker chips wear the same status tone as the `schema mismatch` badge (R9), text carries meaning.
- A service absent from a parent object is marked once at that object, excluded from descendant
  variants (the rule that keeps the 5-consumer nested case readable). Kind conflicts
  (object vs string) stop descent with the truncation sentence. A whole-plane absence
  (`no response schema declared — shipping-api`) is a root-level variant line, never a silent drop.
- Plane headers carry computed counts: `Request · declared by 3 consumers · 2 fields differ`.

Build order (design agent's ranking, kept):

1. `selectSchemaAgreement(state, topic): SchemaAgreementView` — the pure union walk over
   `declaredSchemas` (`AgreementNode {name, agrees, consensus?, variants?, differsInside,
   truncated, children}`; absence-scoping rule above). Tests: agreement, presence/type/required
   conflicts, facet-only difference (the pattern case the .NET taxonomy cannot see), absence
   scoping, degraded, zero-diff invariant. **This is the feature; the component is a render of it.**
2. Extract `typeOf/facetsOf/childrenOf/requiredOf` from `SchemaTree.tsx` into
   `sections/schemaShape.ts`; new `sections/SchemaAgreement.tsx({view})`; stories for common
   (3 consumers/2 fields), ugly (5 consumers, nested, type conflict), degraded, plane-absent.
3. TopicPage wiring: when `entry.schemaMismatch && view.published`, the Contract card renders
   `SchemaAgreement` instead of the SchemaTrees and **hoists to position 1** under the
   VersionSwitcher (design PO question 1, recommendation taken); the interim mismatch banner is
   **deleted** (it also carries a future-tense claim the copy-honesty suite would fail). Healthy
   state: card stays where it is, renders as today.
4. Degraded fallback (mismatch flagged, `declaredSchemas` absent — older aggregator): keep the
   representative tree plus the honest "this catalogue does not publish each service's declared
   schema" copy. All fixed strings into the audited copy module.

Noted as a follow-up, not in scope: the Test Console composes from the representative schema, so on
a mismatched topic a payload one consumer accepts is one another rejects — same fiction, second
place (design PO question 3).

### WP-A3 · re-vendor
Rebuild bundle → `build/mesh-ui.html` → benzene-dotnet embedded resource → Benzene `mesh-ui/` +
website demo. One re-vendor covers Part A and Part B's UI changes together.

---

## Part B — dispatch, end to end

### B0 · Findings that shape the plan

- **The packages are done.** `benzene:mesh:dispatch` handler, gate, options — complete and tested.
  `AwsLambdaMeshServiceDispatcher` (key `AwsLambdaInvoke`) already exists and reuses the discovery
  invoke plumbing. **No new dispatcher is needed.**
- **No IAM change.** The mesh role already holds `lambda:InvokeFunction` on the service Lambdas
  (the interrogation grant). Dispatch changes the payload, not the permission.
- **Five independent blocks** in the deployed example: `dispatchUrl: null` on the UI mount; the
  envelope endpoint's `TopicFilter` excludes dispatch; `UseMeshDispatch`/`AddMeshLambdaDispatcher`
  never called; the DI registry is a permanently **empty** singleton (the real one is produced per
  discovery run and written to S3, never reaching DI — so every dispatch would 404); and no
  `DOTNET_ENVIRONMENT` is set, so the gate reads unset-as-Production and refuses.
- No example anywhere passes a non-null `dispatchUrl` — AwsMesh becomes the first working
  end-to-end reference.

### B1 · benzene-dotnet: `MeshDispatchGuardMiddleware` *(agent-ready; full spec in security report)*

New, in `Benzene.Mesh.Dispatch`, structurally cloned from `MeshRefreshGuardMiddleware` (route
canonicalisation + `IRouteFinder` topic second-match, header-first ordering, minimal denial bodies):

- **Order**: route match → CSRF (`X-Benzene-Dispatch` required header; missing → bare 403) →
  identity (fail closed → 403) → payload bounds (128 KiB body, 32 inner headers → envelope
  `bad-request`) → rate limit (10/min per identity, 30/min per target service, fixed-window,
  in-memory → **envelope** `too-many-requests` + `Retry-After` header).
- The rate-limit refusal MUST be envelope-shaped: the UI reads outer `statusCode` and renders
  `MeshDispatchBlockedError` — a bare HTTP 429 falls into the generic-failure path and reads as
  "broken" instead of "throttled".
- **Identity gap to close**: `OidcSessionGateMiddleware` validates the email and discards it.
  Either the OIDC package stashes the validated email for downstream middleware (cleaner) or the
  guard re-validates the cookie with the same key (acceptable). Without this the audit is blind.
- **Audit (E6)**: one structured log record per attempt via the existing OTel/CloudWatch path —
  email, service, topic, environment, outcome
  (`csrf-denied|no-identity|payload-too-large|rate-limited|gate-blocked|<envelope status>`),
  timestamp, traceId. **Never** the payload or response body.
- `MeshDispatchGuardOptions` defaults per the security report; env-var wiring mirrors
  `BuildRefreshGuardOptions` (lenient parse, guard defaults on unset).
- Honesty note carried into the docs: the in-memory limiter guarantees per-warm-instance only; the
  **hard** flood guarantee is API Gateway's (B3). No distributed limiter — disproportionate.

### B2 · benzene-dotnet: AwsMesh wiring *(agent-ready)*

1. Explicit `ProjectReference` to `Benzene.Mesh.Dispatch` in the Mesh csproj.
2. `ConfigureServices`: `benzene.AddMeshLambdaDispatcher();` and a **scoped** `MeshServiceRegistry`
   factory reading `registry.json` back from the artifact store (absent → empty registry → dispatch
   yields `not-found`, not a crash). Registered after `AddMeshAggregatorWithS3` so last-wins over
   the empty singleton; verify last-wins holds under Benzene.Microsoft.Dependencies.
3. `Configure`, on the API Gateway pipeline, after `UseMeshOidcAuth`:
   `.UseMeshDispatchGuard(guardOptions)` then `.UseMeshDispatch(new MeshDispatchOptions())` —
   **`AllowInProduction` stays unset** (see B4).
4. The envelope endpoint's `TopicFilter` widens to exactly two shapes:
   `benzene:mesh:query:*` OR equals `benzene:mesh:dispatch`. Nothing else — the filter is
   load-bearing (the process-wide handler union means anything wider re-exposes
   `benzene:mesh:aggregate` around the refresh guard). Extend the existing comment.
5. UI mount: `dispatchUrl: "/benzene/invoke"`.
6. Tests: existing `MeshDispatchTest` green; registry-factory unit tests (present/absent);
   pipeline test that dispatch reaches the handler while `benzene:mesh:aggregate` is still refused
   and `query:*` unaffected; gate test that an ungated environment returns envelope `forbidden`
   (rendered as "blocked", not "failed").

### B3 · Terraform *(agent-ready)*

1. `DOTNET_ENVIRONMENT = var.mesh_environment` (new variable, default `"Development"` for this demo
   estate) on the mesh Lambda's environment block. This is what turns dispatch on in dev — the gate
   itself is not touched.
2. Method-level throttle on the dispatch route: new
   `mesh_dispatch_throttling_rate_limit = 2` / `mesh_dispatch_throttling_burst_limit = 5`
   variables + `method_settings` block, alongside the existing stage-wide 10/20. This layer refuses
   work **before it is billed** and carries the hard rate guarantee.
3. No IAM change (B0).

### B4 · RULING (from the security design, adopted): production dispatch stays hard-off

No `AllowInProduction`, no interim email-grain production allowlist. E5 (read- vs write-shaped
topics) is unresolved, so every topic must be treated as write-shaped; E4's default for production
`dispatch-write` is nobody. An interim allowlist would contradict E2 and create a config surface
that roles would have to deprecate. Dev gets dispatch by being dev — behind login, CSRF, bounds,
rate limits, gateway throttle, and audit. Production dispatch opens only with roles (E3/E4) and the
E5 marker, together with replay-proof one-shot tokens (deferred with it).

### B5 · benzene-ui: the client half *(agent-ready)*

1. `dispatchMessage` sends `X-Benzene-Dispatch: 1` — exactly as `postRefresh` sends its header.
2. Verify the `too-many-requests` envelope renders as a distinct "throttled, retry in N" message
   through the existing `MeshDispatchBlockedError` path (it should; add a test).
3. Re-vendor with WP-A3.

### B6 · End-to-end verification (manual, on the deployed stack)

Login → Test Console shows Send (proves `data-dispatch-url`) → send a benign read-shaped topic →
target's real response renders → CloudWatch shows the target Lambda invoked by the mesh role and
one `benzene.mesh.dispatch.audit` record → eleventh rapid send in a minute returns the throttled
message, not a generic failure.

---

## Part C — "Changes doesn't work": finding, no fix required

The drift pipeline is fully wired in the deployed example. The behaviour is semantic:

- The **first run after a deploy has no previous catalogue**, so `changes` is empty by design.
- Each run **overwrites the baseline**, so a detected change is visible only until the next
  scheduled run — typically one interval.
- The demo services are **static**: nothing drifts between runs, so the ledger is legitimately
  empty — which reads as "doesn't work".

If a persistently-visible ledger is wanted, that is a product decision, not a fix: an accumulating
change-history artifact instead of (or beside) the run-over-run diff. Parked for the product owner;
NOT in this plan's work packages.

## Sequencing

```
WP-A1 (dotnet: declaredSchemas)        WP-B1 (dotnet: dispatch guard)     ← parallel, different files
        ↓                                      ↓
WP-A2 (ui: union tree)                 WP-B2 (dotnet: AwsMesh wiring) + WP-B3 (terraform) + WP-B5 (ui client)
        ↘                                      ↙
          WP-A3 / one shared re-vendor across all four repos
                        ↓
               WP-B6 manual verification on the deployed stack
```

A1∥B1 and A2∥(B2,B3,B5) are safe to run as parallel agents — disjoint files. The re-vendor must be
single and last.
