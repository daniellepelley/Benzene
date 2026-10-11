# Side-by-side service versions — proposal

**Status: proposal, moved out of the spec** (spec-simplification-plan D19, 2026-10-11). Version-aware
routing between two co-deployed releases of one service. The identity layer it needs is specified,
Provisional, in [`docs/specification/service-versions.md`](../docs/specification/service-versions.md);
what follows is the unbuilt part, kept here until a port builds it. Section numbers below are the
ones this text had as `versioning.md` §5.4, §5.7, §6 and §7. Every section reference below (§2.x,
§3, §4.x, §5.x) is to that old `versioning.md`, as of `spec-0.2.0`; its §2–§4 are now the short
`versioning.md` and the .NET versioning-internals cookbook, its §5.1–§5.3 and §5.5–§5.6
`service-versions.md`. "design-principles.md §5.1" is now `docs/guides/design-principles.md` §5.

## 1. The idea (versioning.md §5 intro)

**Status: proposed.** Mechanisms A and B both keep **one deployment** of a service and absorb the
version difference inside it. Mechanism C is the other axis entirely: leave the old deployment
running, deploy the new one **alongside** it, and route each message to whichever deployment speaks
its version. Nothing is replaced until the operator decides to retire it.

Good fit when the change is too large or too risky to absorb by casting: a rewritten
implementation, a genuine behavior change (not just a data-shape change), a migration that needs
independent rollback, or a gradual cutover with real traffic on both versions at once. It is the
deployment-shaped answer to the same problem §3 and §4 answer in code.

## 2. Routing a message to the right service version

The enabling decision was already made in §2: **the version travels as transport metadata, never
inside the payload.** Metadata is the one layer infrastructure routers can see, so most transports
can route by version with no framework involvement at all. Had the version lived in the body (as in
the source project §2 describes departing from), almost nothing below could work.

| Transport | Version-based routing | How |
|---|---|---|
| HTTP / Kubernetes | Yes, natively | `/v{version}` path or `benzene-version` header rule at the ingress/gateway — ordinary canary machinery |
| SNS | Yes | Subscription **filter policies** on the message attribute the version header maps to; one queue per version |
| EventBridge | Yes | Rules pattern-match the embedded header (transport-bindings.md's `benzene-headers`) |
| Bare queue (SQS, etc.) | **No** — a queue has no routing layer | Front it with a topic/bus that has one, **or** route producer-side (below) |
| Direct invocation (Lambda invoke, gRPC) | Producer-side only | The caller chooses the callee, so the outbound route must be version-aware |

Two placements, and an implementation **MAY** offer either or both:

- **Infrastructure-side** (preferred where available). Producers publish one topic with a version
  header and stay entirely ignorant of how many versions are deployed. Cutover is a routing-rule
  change with no redeploy of anything.
- **Producer-side.** The outbound route resolves `(topic, version) → destination` rather than
  `topic → destination`. This is the only option for bare queues and direct invocation. *(Informative,
  .NET: this is a real gap today — `OutboundRoutingBuilder.Route` keys on topic alone, even though
  `SendAsync(topic, request, version)` already stamps the header. A version-aware route key is the
  one framework change Mechanism C needs.)*

Whichever placement is used, the **absent-version default (§2.2) MUST be encoded in the routing
rule**: a message with no version signal routes to the topic's default version — by convention the
oldest still accepted — so producers that predate versioning keep reaching the deployment that can
still serve them, rather than silently landing on the newest.

## 3. Sharp edges

These are constraints on the pattern, not gaps in it, and an implementation guide **MUST** state
them — each one is a live production hazard that routing alone does not address:

- **Event fan-out double-processing.** Side-by-side is clean for *commands*, which have one intended
  recipient. For a subscribed *event*, if two versions both subscribe, the event is processed twice —
  duplicate side effects, not a duplicate read. Exactly one version MUST own a given event
  subscription at a time, and that ownership moves at cutover.
- **Shared state.** Two versions writing one store relocates the breaking change from the wire to
  the database rather than removing it. Side-by-side deployment addresses *contract* compatibility;
  the store still requires expand/contract discipline, and no routing scheme substitutes for it.
- **Unbounded accumulation.** Versions are immutable and additive, so without a retirement policy an
  estate grows service versions forever. Retirement is an operator decision the mesh should make
  visible (which versions still receive traffic) but MUST NOT make automatically.

## 4. Choosing between the mechanisms

| | §3 Handler-version dispatch | §3.1 Casting-handler sugar | §4 Transparent casting | §5 Side-by-side versions |
|---|---|---|---|---|
| **Deployments per service** | One | One | One | **One per live version** |
| New framework code required | None (already shipped) | None (application-level) | Request/response mapper decorators + §4.4 redesign (both shipped) | Version-aware outbound routing (§5.4) + mesh identity (§5.5) — **neither shipped** |
| Handler code duplication | Real, per version | One thin forwarding handler per retired version | None — one handler, always the latest | None — each deployment only knows its own version |
| Where the version difference is absorbed | In the handler set | Explicit, in the forwarding handler | Implicit, in registered `ISchemaCaster`s | **Nowhere in code** — in the routing layer and the deployment topology |
| Rollback granularity | Redeploy the service | Redeploy the service | Redeploy the service | **Per version, independently** |
| Good fit when | Versions genuinely behave differently, not just shaped differently | A quick bridge for one or two retired versions | Many long-lived producer versions, pure data-shape drift, want zero per-version handler code | A rewrite, a risky migration, or a gradual cutover with real traffic on both at once |
| Main cost | Duplicate handler code | A forwarding handler per retired version | Casters must exist in both directions (§4.2.1) | Running, observing and eventually retiring N deployments; the §5.7 hazards |

The mechanisms **compose**, and the useful combination is C with A or B rather than either alone: a
side-by-side deployment handles the genuine behavior change, while casting inside each deployment
absorbs shape-only drift, so C's deployment count tracks *behavioral* versions rather than every
schema revision. C alone, with no casting, means a new deployment for every payload change — which
is where an estate accumulates versions fastest and §5.7's retirement problem bites soonest.

## 5. Open questions for the implementation pass

- **Multiple simultaneous canonical versions per topic.** §4.1 assumes one canonical version per
  topic (the resolved handler's declared type). A topic with more than one live canonical version
  (rare, but the already-shipped `PayloadSchemaVersions.ToSchemas` is an array, suggesting the
  original design considered it) needs an explicit disambiguation rule before implementation —
  candidate: match the schema caster whose `ToType` equals `messageHandlerDefinition.RequestType`
  exactly, iterating `ToSchemas` until found.
- **HTTP route-parameter naming collision.** If an application already has a domain route parameter
  literally named `version` for unrelated reasons, the default HTTP `IMessageVersionGetter` would
  misfire. Needs either a documented reserved-name warning (similar to `/benzene/` prefix
  reasoning, design-principles.md §5.1) or a configurable parameter name.
- **Content negotiation vs version negotiation interplay.** §4.2's symmetric-by-default response
  versioning and the existing `IMediaFormatNegotiator<TContext>` (format: JSON/XML/...) are
  orthogonal today; confirm no ordering dependency once both decorators are real (format selection
  reads `content-type`/`accept`, version selection reads `benzene-version`/route — no shared state).
- **Conformance fixtures.** Once implemented, add version-casting cases to
  `docs/specification/conformance/` (envelope-cases.json sibling) so other-language ports can
  verify their casting chain composition against the same fixtures as the .NET reference.

Specific to Mechanism C (§5):

- **A contract-only fingerprint (§5.3, row 4).** Reporting "these two versions differ but their
  contracts are identical" — the signal that tells a consumer team it has nothing to do — needs a
  digest over the contract *alone*. `descriptorHash` cannot serve: `serviceVersion` participates in
  it (mesh.md §2.2), so two declared versions always hash differently even when their contracts
  match. Two candidates, both normative changes to a conformance-pinned definition and so needing
  their own cross-port pass: **narrow** `descriptorHash` by adding `serviceVersion` to its exclusion
  list (cleaner, but changes an existing hash's value in every port), or **add** a second
  contract-only digest alongside it (purely additive, at the cost of two hashes to explain). The
  first also has a subtlety worth checking: with `serviceVersion` excluded, the hash no longer
  changes on a version bump, so anything today relying on "hash changed ⇒ redeploy" would need to
  key on the version identity instead.
- **Mixed identity sources in one estate.** §5.2 resolves per service, so one service may key on a
  declared `1.4.2` while another keys on a substrate revision, and a third has no version identity at
  all. That is intended (a port cannot force declaration), but a catalog rendering all three needs a
  presentation rule — most likely: mark a substrate-derived identity as such rather than letting it
  read like a name someone chose, and render a service with no version identity exactly as services
  are rendered today, with no empty "version" affordance implying something is missing.
- **What counts as the "default version" for absent-version routing.** Within one service the
  default is now declared (core-concepts §2, §2.2 above), and §5.4 requires routing rules to encode
  it across deployments. Nothing yet carries that declaration to the mesh, so across releases it is
  still held in an operator's head and duplicated into a routing rule. Candidate: an explicit marker
  on the descriptor, so the mesh can show which version absent-version traffic actually lands on, and
  flag the case where two live versions both believe they are the default.
- **Where the version identity is applied.** §5.5 keys the collector by `(service, serviceVersion)`,
  but every existing read model, artifact document and UI surface is keyed by service name alone
  (`services/{name}.json`, the topic graph's node identity, issue fingerprints — mesh.md §4.1 —
  which include `service` but not its version). Settle whether those become version-keyed, stay
  service-keyed with version as an attribute, or split; doing this piecemeal per surface is how the
  two identities drift apart.
- **Retirement, and evidence for it.** §5.7 requires retirement to be an operator decision, and
  mesh.md §4.2's declared-vs-observed layer already distinguishes a declared edge that no trace has
  exercised. Confirm that signal is sufficient to answer "is anything still calling v1?" — it is the
  question the whole mechanism eventually turns on, and the one an operator will not retire without.
- **Conformance fixtures for sibling versions.** The collector cases currently register one
  descriptor per service. Add cases pinning each row of §5.3's table: two descriptors differing only
  in `serviceVersion` produce two catalog entries rather than one overwriting the other (rows 3–4);
  two differing in contract under one declared version still report drift (row 2); and — the case
  that motivated this mechanism's redesign — **two differing only in `serviceVersion`, with
  byte-identical topics and schemas, still produce two entries** (row 4). That last one is the
  regression guard against re-deriving identity from contract content.
