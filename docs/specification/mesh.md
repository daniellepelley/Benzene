# Mesh Contracts

**Module — Stable.** Optional at Core level; its service-side feeds are required by the
[Cloud Service Profile](cloud-service-profile.md) (R6).

Benzene Mesh is the *application-level* mesh: every service **declares its full contract** — the
topics it serves, the events it publishes and the messages it sends to topics others define — so
the fleet graph (catalog, who-calls-whom) is knowable statically, from registrations alone, before
a single instance is deployed or a single message has flowed. Running code supplies a second,
independent signal — health (§5) and, from the trace feed (§3), which declared edges are actually
being exercised (§4.2) — but running code is never the *source* of the graph, only a witness to it.
This document specifies everything that crosses a process boundary to make that work identically
across language ports.

At Core level, mesh is optional, and so is each of its feeds; the normative degradation rule of
§6 applies to every section here. A service claiming the
[Cloud Service Profile](cloud-service-profile.md) MUST provision the service-side feeds (its
R6) — for such a service, §6 governs runtime degradation, not whether the feeds exist.

## 1. Self-description

A service describes itself once: the [Contract Document](contract-document.md), derived from its
registrations — `requests` from its handlers, `events` and `sends` from its event and send
declarations (contract-document §3.3) — and served at the reserved `benzene:spec` topic
(contract-document §7). The mesh has no self-description of its own.

## 2. Registration

The body of a `benzene:mesh:register` message (§4) is the service's Contract Document — the
same document `benzene:spec` serves, including the deployment-side fields (`runtime`,
`placement`, `profile`, `degraded`, contract-document §1) the service knows. It carries
`registrationHash`, which a collector trusts: it MAY recompute it to report a mismatch, but never
rejects a registration for one, and compares heartbeats with the carried value (§5).

- **Identity.** `service` and, where declared, `serviceVersion` are the catalog key (§4);
  `versionScheme` says how to order versions. Which immutable release a `serviceVersion` names, and its order, are the
  [Service Versions](service-versions.md) module (Provisional); a document that omits
  `serviceVersion` is one service version, so an estate whose services never declare one is
  unaffected.
- **Declared, never inferred.** `events` and `sends` come only from explicit declarations
  (contract-document §3.3). That is what makes the producer side of the graph as reliable as the
  handler registry makes the consumer side.
- **A reduced registration.** A producer that cannot determine a member names it in `degraded`,
  so that its empty list does not assert "none" (contract-document §1).
- **What a collector requires.** Only `service` (→ `bad-request` when missing). Every other member
  may be absent; the REQUIRED fields of contract-document §1 bind producers, and its dialect rule
  does not apply to a collector, which does not interpret schemas. A member that is absent, or
  that `degraded` names, is **unknown**: it contributes no edges, and a trace on one of its topics
  is not undeclared (§4.2).

## 3. TraceEvent

One pipeline invocation as the mesh sees it — semantic (topic + Benzene status), not
transport-shaped.

```json
{
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "spanId": "00f067aa0ba902b7",
  "parentSpanId": "0af7651916cd43dd",
  "service": "orders",
  "instanceId": "orders-7f9c",
  "topic": "order:create",
  "topicVersion": "v2",
  "status": "validation-error",
  "exceptionType": "System.InvalidOperationException",
  "durationMs": 12.4,
  "startedAt": "2026-07-16T09:14:03.120Z",
  "correlationId": "abc-123"
}
```

- `traceId`/`spanId`/`parentSpanId` are the W3C Trace Context fields (32/16/16 lowercase hex).
  An inbound `traceparent` header (wire-contracts header conventions) joins the existing trace:
  its trace-id is adopted and its parent-id recorded. An absent or malformed header — wrong
  segment count or length, non-hex, or the all-zero ids the W3C spec defines as invalid — MUST
  yield a fresh trace-id and no parent: a bad caller header degrades correlation, never the
  invocation. Pinned by the `traceparent` section of `conformance/mesh-trace-cases.json`.
- Outbound propagation: a downstream Benzene call carries
  `traceparent: 00-<traceId>-<spanId>-01` built from the calling invocation's span — required of a
  profiled service by the [Cloud Service Profile](cloud-service-profile.md)'s R8. This is what lets a
  collector correlate an *observed* call with the declared edge it exercises (§4.2) — propagation
  feeds observation, not the graph itself (§4), which comes from the registration (§2) regardless of
  whether any call has ever propagated a trace at all.
- `status` is the Benzene status verbatim (wire-contracts.md §3); empty only when no downstream
  middleware produced a result (a wiring gap, reported as-is).
- `exceptionType` *(optional)* — when the invocation's failure originated in a
  thrown exception, the exception's language-native **type name** (e.g. a fully-qualified CLR or Java
  class name, a Go error type). Never the exception message, stack trace, or any payload-derived text —
  the type is a stable, non-sensitive discriminator (the same classification rule the health-check
  plane uses). Omitted for non-exception failures and by emitters that don't capture it; a collector
  MUST accept its absence.
- `correlationId` mirrors the `x-correlation-id` header when present.
- Coverage MUST be structural: because the router already converts a missing handler, a request
  conversion failure, and a handler panic/exception into results (core-concepts.md §5), every
  routed invocation yields exactly one TraceEvent. Pinned by the `invocations` section of
  `conformance/mesh-trace-cases.json`.

## 4. Collector topics

A collector is an ordinary Benzene service serving these topics over any envelope-capable
transport (transport-bindings.md):

| Topic | Body | Success payload |
|---|---|---|
| `benzene:mesh:register` | Contract Document (§2) | `{"accepted":1}` |
| `benzene:mesh:heartbeat` | Heartbeat (§5) | `{"accepted":1}` |
| `benzene:mesh:traces` | `{"events":[TraceEvent…]}` | `{"accepted":<count>}` |
| `benzene:mesh:issues` | IssueBatch (§4.1) | `{"accepted":<count>}` |

- `service` is REQUIRED on register, heartbeat, and issues → `bad-request` when missing. A
  `benzene:mesh:traces` or `benzene:mesh:issues` batch of any size, including empty, MUST be accepted.
- **The catalog key.** A collector keys its catalog by `service`; one that implements the
  [Service Versions](service-versions.md) module keys it by `(service, serviceVersion)` instead. A
  registration that omits `serviceVersion` has one service version, so for an estate whose services
  never declare one the two keys agree.
- Re-registration replaces the previous registration for **that key** wholesale, including the claim
  to consume *and* the claim to produce each topic — a redeploy that drops a topic from `requests`
  drops the consumer edge with it, and a redeploy that drops a topic from `events` or `sends` drops
  the provider edge with it, the same rule applied symmetrically to every declared list (§2). In a
  collector implementing Service Versions it MUST NOT disturb a *different* service version of the
  same service: two releases deployed side by side are two entries, and registering one never
  deletes the other.
- **The producer/consumer graph MUST be built from the latest registration alone** — `events` and
  `sends` for providers, `requests` for consumers (§2). A service that publishes an event is that
  topic's provider exactly as a service that sends to a topic is; `benzene:` entries are ignored
  (contract-document §1). A collector MUST report this graph
  in full for a service that has registered but never sent or received a single message: the graph
  is the declared contract, not a summary of traffic. Trace parentage (§3) MUST NOT be used to admit
  an edge into this graph, add a consumer/provider the registration didn't declare, or remove one it
  did — its role is §4.2, entirely separate from graph membership.
- **Graph membership is by `service`, not by `(service, serviceVersion)`.** A topic's providers and
  consumers name services; two live versions of one service both declaring a topic contribute one
  edge, not two. The catalog keys by version so that registrations don't overwrite each other
  (above); the graph answers "who talks to whom", a question about services, and duplicating a node
  per version would answer it worse. The per-version breakdown belongs to the service view, where
  the versions are the subject rather than the noise.

Sender behavior (normative for ports): trace export MUST be asynchronous, non-blocking, and
lossy under backpressure — a full buffer drops events, a failed send drops the batch, and no
mesh feed may ever fail, slow, or block the invocation it observed.

Collector behavior is pinned by `conformance/mesh-collector-cases.json`. The collector fixtures
observe it through three query read models — `benzene:mesh:query:fleet`, `benzene:mesh:query:service`
and `benzene:mesh:query:topic` — which are **Provisional**: their shapes are asserted only as far as
the fixtures need, and full pinning follows 1.0. Other `benzene:mesh:query:*` topics a collector
serves are its own.

### 4.1 Issues (`benzene:mesh:issues`)

Optional on both sides (§6 applies): a collector that does not implement it is unaffected, and a
service that does not emit it is reduced. It adds no Cloud Service Profile requirement.

The pipeline-native failure feed: where traces record *what happened*, an issue records *what is
wrong* — a deduplicated, classified failure signature emitted by the pipeline itself, which
uniquely holds the wire status and the thrown exception at the moment of failure.

```json
{
  "service": "orders",
  "issues": [
    {
      "fingerprint": "9f2c1a7e0d4b86513a9e2f70c4d1b8a2",
      "classification": "exception",
      "service": "orders",
      "topic": "order:create",
      "version": "v2",
      "transport": "sqs",
      "status": "service-unavailable",
      "exceptionType": "System.Net.Http.HttpRequestException",
      "count": 12,
      "firstSeen": "2026-07-25T09:14:03Z",
      "lastSeen": "2026-07-25T09:41:12Z",
      "exemplarTraceIds": ["4bf92f3577b34da6a3ce929d0e0e4736"],
      "resolutionHint": "deserialization"
    }
  ]
}
```

- **Batch-level `service` is REQUIRED** (→ `bad-request`), even though each issue also carries
  `service` (an aggregating relay may batch for several services): an **empty batch is the feed's
  liveness assertion** — "feed alive, nothing failing" — and must be attributable. Emitters
  SHOULD flush on an interval even when empty, so a collector can distinguish a quiet wired
  service from an unwired one; a collector MUST accept an empty batch.
- **`count` is a DELTA**: occurrences since the emitter's previous successful flush, never a
  cumulative total. Collectors merge by fingerprint: `count += delta`, `firstSeen = min`,
  `lastSeen = max`, exemplars keep the newest (≤3), other fields latest-wins. Delta semantics
  make merge restart-proof and need no instance identity on the wire; a dropped batch loses its
  delta — lossy by design, the same trade as `benzene:mesh:traces`.
- **`fingerprint` derivation is normative**: the lowercase hex of the first 16 bytes of SHA-256
  over the UTF-8 bytes of `service|topic|version|classification|discriminator` (pipe-joined),
  where `version` is the empty string when absent and `discriminator` is `exceptionType` when
  present, else `status`. `transport` is deliberately excluded — the same failure over two
  transports is one issue. Cross-language fingerprint equality holds only for non-exception
  classes (`exceptionType` is language-native); same-service equality across instances and
  restarts is the property that matters for merge. **Neither the problem-document `code` nor
  `type` (wire-contracts.md §1.3, §3.1) participates in the fingerprint** — both are open,
  per-error or per-response identifiers, and admitting either would explode issue cardinality and
  defeat the merge above.
- **`classification` is a closed vocabulary** — `exception`, `validation`, `config-wiring`,
  `dependency`, `contract-drift`, `unclassified` — assigned by normative precedence, evaluated
  in order against the invocation's Benzene status (wire-contracts.md §3) and captured exception
  type:
  1. `bad-request`, `validation-error` → **validation** (even when an exception type is present —
     a deserialization or argument exception is still a validation issue; the exception type
     remains the fingerprint discriminator);
  2. exception type present → **exception** (a thrown-and-converted failure classifies by its
     throw, not its mapped status);
  3. `not-found`, `unauthorized`, `forbidden`, `not-implemented`, or an **empty status** (§3: a
     wiring gap) → **config-wiring**;
  4. `service-unavailable`, `timeout`, `too-many-requests` → **dependency**;
  5. `unexpected-error` → **exception**;
  6. any other failing status → **unclassified** (an honest fallback beats a lying class).
  `contract-drift` is never produced by this table — it is reserved for catalog/heartbeat-derived
  issues (registration-hash mismatch, schema divergence) and the undeclared-edge case §4.2 defines
  (a trace names a topic absent from the caller's `events` and `sends` or the handler's
  `requests`), filed in
  this same shape by a collector or reader, so emitter implementers should not hunt for its trigger.
- `exceptionType` is the language-native type name only — never a message, stack trace, payload,
  or header. `resolutionHint` is an optional key into a remediation catalog, never prose;
  registered keys so far: `no-handler` (routing genuinely matched no handler — distinguishes a
  wiring `not-found` from a handler-returned business `not-found`), `deserialization` (the request
  could not be read into the handler's request type). Unknown keys MUST be tolerated (readers
  fall back to classification-level guidance).
- Sender rules are `benzene:mesh:traces`' rules (§4): asynchronous, non-blocking, lossy, dedup at source
  (per-occurrence events MUST NOT be sent), and the feed may never fail, slow, or block the
  invocation it observed.

Pinned by `conformance/mesh-issue-cases.json` — required only for collectors claiming the issue
feed (see `conformance/README.md`).

### 4.2 Declared vs. observed — liveness and drift

The graph (§4) is declared and does not need traffic to exist. Traces still matter — they are the
*only* way to know whether a declared edge is actually being exercised, and the only way to notice
an edge nobody declared. Both are collector-derived read models over the same two inputs
(the registration's declared lists and `TraceEvent` parentage), never a change to either input:

- **Unobserved** — a declared edge (in `requests`, `events` or `sends`) with no corresponding trace parentage
  within the collector's retention window. This is a **decommission candidate**, not a fact: trace
  export is lossy by design (§4), so absence of evidence is not evidence of absence, only a prompt
  to go check. A collector MUST report *last observed at* (or its absence) per edge rather than
  collapsing it to a boolean, so a reader can judge staleness for itself.
- **Undeclared** — trace parentage between two services on a topic that is not present in the
  caller's registered `events` or `sends` (or, symmetrically, a trace naming a topic absent from
  the handler's registered `requests`). A reserved (`benzene:`) topic is never undeclared: no
  document lists one (contract-document §1). This is **`contract-drift`** (§4.1's classification vocabulary,
  which already reserves this class for exactly this collector-derived case): the declared contract
  and the running system disagree, and a reader needs to know which one is stale — the registration
  the last deploy forgot to update, or a caller that shouldn't be doing what it's doing.

Neither signal is itself a topology edge. A view MAY render "declared, unobserved" and "observed,
undeclared" states distinctly from a confirmed (declared *and* observed) edge, but the graph (§4)
itself contains only what's declared, unconditionally.

## 5. Heartbeat

The health-check aggregate response (wire-contracts.md §5) reused byte-for-byte, wrapped with
identity:

```json
{
  "service": "orders",
  "serviceVersion": "1.4.2",
  "instanceId": "orders-7f9c",
  "registrationHash": "sha256:…",
  "sentAt": "2026-07-16T09:14:03Z",
  "health": { "isHealthy": true, "healthChecks": { "db": { "status": "ok", "type": "postgres" } } }
}
```

`serviceVersion` is the instance's release, omitted when it declares none. A heartbeat whose
`registrationHash` differs from the registered document's means the instance runs a contract the
collector hasn't learned. The collector MUST surface the mismatch
(the Go collector reports per-instance `hashMatches`) rather than silently keeping stale topics.

In a collector implementing [Service Versions](service-versions.md), this comparison is scoped to
**one service version**: the heartbeat's hash is compared against
the document registered for the *same* `(service, serviceVersion)` key, never against a different
release of the same service. Two releases deployed side by side legitimately report different hashes,
and a collector that compared across them would report drift for every side-by-side deployment — the
opposite of what this rule is for. A heartbeat carrying a `serviceVersion` no document has
registered is an unregistered instance, not a hash mismatch. A collector without the module
compares against the service's one registration.

## 6. Degradation (normative)

Every mesh feed — registration, heartbeats, traces, issues — is independent and
optional, on both sides:

- **Service side**: an unreachable collector, a failing or absent exporter, or an absent registry
  each reduce the mesh and MUST NOT affect the service's
  own traffic in any way.
- **Collector side**: partial fleets MUST be accepted and rendered as reduced. Traces from a
  service that never registered present it as known-but-reduced (missing registration, which the
  collector read models call the `descriptor` feed); a
  registered service with no traffic is a catalog entry with no stats; no heartbeats means
  unknown health. A missing feed MUST NOT fail ingestion or queries.

## 7. Conformance

Fixture files in [conformance/](conformance/README.md) pin this document; their formats and the
canonical mesh handlers are documented there. A port that implements mesh MUST pass
`mesh-trace-cases.json`, and the contract-document producer groups (contract-document §8), whose
document is its register body; a port that additionally implements a collector MUST pass
`mesh-collector-cases.json` (including the graph-from-registration cases — a registered service with
zero traffic reporting its full provider/consumer graph — and the declared-vs-observed cases of
§4.2), and a collector that additionally claims the §4.1 issue feed MUST pass
`mesh-issue-cases.json` (optional — a collector without it stays collector-conformant, and picking
up the undeclared-edge trigger of §4.2 is bundled with it, not a separate obligation). A port that
implements neither is unaffected at Core level — mesh is an optional module there, and the Core
spec creates no obligation to implement it, only the obligation to implement it *compatibly*.
Supporting the [Cloud Service Profile](cloud-service-profile.md), however, requires the
service-side feeds, so a port that wants its services to claim the profile implements §§2–3 and §5
and passes the service-side fixtures above.

## 8. Conformance language note

Per the repository's one design rule, everything in §§1–6 is a Benzene *concept* — wire shapes
and cross-process behavior. How a port derives its document (attribute scanning vs explicit
registration), how a collector stores state, and what a view renders are *idioms* and stay out
of this document.
