# Specification Review — maintainer pass, 2026-07-25

**Status:** OPEN BACKLOG — items captured, none decided. Each needs investigation, then a ruling.
**Last Updated:** 2026-07-25
**Purpose:** Capture the maintainer's review of the Benzene specification draft
(`docs/specification/`) as a worked backlog: what was said, what the spec currently says (grounded,
with references), the question each item poses, and a first assessment to argue with. Decisions are
deliberately NOT taken here.

**Why the timing matters:** 1.0 is imminent (`work/1.0.0-release-status.md`: 26/29 release-plan
items closed; `version.txt` still `0.0.2`, no tag). Items 2–4 below change **wire contracts, header
names and topic ids** — precisely the things that cannot be changed after 1.0 without a major
version and a migration story. **Anything on this list that is going to happen should happen before
the tag.** Item 1 is additive and could follow later, but is cheaper now too.

---

## Summary

| # | Item | Kind | Breaking? | Settle before 1.0? |
|---|---|---|---|---|
| 1 | `CancellationToken` on message handlers | API surface | Additive if done right | Preferable |
| 2 | Error payload standardisation (RFC 9457 / `ProblemDetails`) | Wire contract | Yes, if shape changes | **Yes** |
| 3a | `traceparent`/`tracestate` — spec or profile? | Spec scoping | Doc-only | **Yes** (cheap) |
| 3b | `x-correlation-id` → a self-contained correlation add-on | Spec scoping + packaging | Doc-only; code is additive | **Yes** for the doc call |
| 3c | `topic` header naming/consistency | Wire contract | Yes, if renamed | **Yes** |
| 3d | `benzene-status` — gRPC-only, is it spec? | Spec scoping | Doc-only | **Yes** (cheap) |
| 3e | `content-type` | Review only | No | Low risk |
| 3f | `benzene-version` → `version`? | Wire contract | Not yet — **unimplemented**, so free now | **Yes** (free window) |
| 4 | `benzene` prefix on all reserved topics | Wire contract | Yes | **Yes** |

Items 3c, 3f and 4 are **one naming decision applied three times**. Decide the principle once
(§5), then apply. Doing them piecemeal is how inconsistency gets shipped.

---

## 1. `CancellationToken` on message handlers

**Maintainer:** "The message handler could take in a cancellation token. There is a cancellation
token if it exists on the scope, so it may be possible to pass that into the message handler. Should
that be a good idea? Some things don't have a cancellation token — those would just have a null
cancellation token and it would always stay active, which I guess is fine."

**Current state:** `IMessageHandler<TRequest,TResponse>` (`src/Benzene.Abstractions.MessageHandlers/
IMessageHandler.cs`) takes the request only. No `CancellationToken` appears anywhere in the
abstractions or `Benzene.Core.MessageHandlers` source.

**The question:** should a handler receive the host's cancellation signal, and how does that reach
it without breaking every existing handler or bolting a parameter onto an interface that most
transports can't populate meaningfully?

**First assessment (to argue with):**
- The value is real and specific: ASP.NET Core gives `HttpContext.RequestAborted`, Azure Functions
  passes a `CancellationToken`, a worker/hosted service has the host's stopping token, and Lambda
  has `ILambdaContext.RemainingTime` (a *deadline*, not a token — but a token is derivable). A
  handler doing a long DB call or an outbound HTTP call should be able to abandon work the caller
  already gave up on. Today it cannot.
- The transports genuinely differ, so **`CancellationToken.None` is the honest fallback** — that is
  exactly what "always stays active" means, and it is the framework-standard no-op. This is the
  right instinct.
- **The design question is where it lives, not whether.** Three candidate shapes:
  1. **A second overload/parameter on the handler interface** — most discoverable, but touches the
     single most-implemented interface in the library, and forces a choice for every existing
     handler. With C# default interface members this can be made non-breaking, but the ergonomics
     of two near-identical interfaces are poor.
  2. **On the context** (`context.CancellationToken`), resolved by middleware from the host —
     zero interface churn, consistent with how Benzene already carries per-message state, and it
     composes with the existing pipeline. But it is less discoverable, and cuts against the
     handler-signature purity the library values.
  3. **Injected via DI as a scoped accessor** (the `PresetTopicHolder`/`MessageErrorState` pattern
     already used in `Benzene.Core.MessageHandlers`) — no interface change at all, testable, and
     precedent exists in this codebase.
- **Cross-cutting consequence:** if handlers get a token, the outbound client interfaces
  (`IBenzeneMessageClient` and friends) arguably should take one too, or cancellation stops at the
  handler boundary and the benefit is halved.
- **Watch:** context purity. The `AGENTS.md` rule says `TContext` describes the transport message's
  shape. A cancellation token is arguably ambient host state, not message shape — which points at
  option 3, or a deliberate documented exception for option 2.

**Needs:** a short design note comparing the three shapes against existing conventions, plus a
survey of what each supported host can actually supply. Route to the **core-product-owner** with
**architecture-reviewer** input.

---

## 2. Error payload — is there a better standard to follow?

**Maintainer:** "The error payload is fairly basic and works to a certain extent. Investigate
whether there is a better error payload and whether there is some standardisation it would be
better for Benzene to follow. I think I have seen ASP.NET use something with fields like details and
error."

**Current state** (`docs/specification/wire-contracts.md` §1.3): on failure the response `body` is

```json
{ "status": "not-found", "detail": "No handler found for topic order:create" }
```

with `type`, `title`, `instance` **reserved for RFC 7807 alignment** (writers MAY emit as null or
omit). Clients recover `errors` from `detail` — the result's error messages **joined with `", "`**.

**The question:** adopt a fuller standard, and if so which?

**First assessment (to argue with):**
- The thing the maintainer is remembering is almost certainly ASP.NET Core's **`ProblemDetails`**
  and, more pointedly, **`ValidationProblemDetails`** — which carries
  `errors: { "fieldName": ["message", …] }`. The underlying standard is **RFC 7807**, now
  **obsoleted by RFC 9457** (Problem Details for HTTP APIs). Benzene's spec already *claims*
  7807 alignment, so this is less "adopt a standard" than "finish adopting the one we named" —
  and update the citation to 9457.
- **The concrete defect is information loss.** Joining errors with `", "` destroys structure that
  the pipeline had at the moment of failure. Validation is exactly where per-field structure
  matters, and Benzene has first-class validation (`Benzene.Validation*`, FluentValidation and
  DataAnnotations integrations) — so the framework knows *which field failed* and then throws that
  away on the wire. Round-tripping by splitting a joined string is fragile by construction.
- This connects to work already shipped: the mesh issue feed (`docs/specification/mesh.md` §4.1)
  built a **closed classification vocabulary** including `validation`, and `MessageErrorState`
  carries converted-exception type + hint. There is an opportunity for one coherent error model
  rather than two half-models.
- **Constraints to respect:** (a) Benzene is transport-neutral, and RFC 9457 is *HTTP* problem
  details — adopt the shape, don't inherit HTTP-only semantics (`type` as a dereferenceable URI is
  optional and often noise); (b) the spec is cross-language (Go port), so any shape must be trivial
  to produce elsewhere; (c) secret-safety — the existing rule that exception *messages* never leak
  into error data (see `HealthCheckError`) must survive.
- **Provisional direction:** keep `status` + `detail`, add a structured `errors` field
  (field → messages), formally adopt RFC 9457 naming, and keep `type`/`title`/`instance` optional.
  That is additive-shaped for readers but a real change for writers and conformance fixtures.

**Needs:** a comparison note (RFC 9457 / ASP.NET `ValidationProblemDetails` / gRPC `google.rpc.Status`
+ error details / JSON:API errors), a proposed Benzene shape, and the conformance-fixture impact.
Route to **core-product-owner** + **validation-product-owner**.

---

## 3. Header conventions — what is actually spec, and what is add-on?

**Maintainer's framing:** several headers "are very much dependent on the middleware used and are
perhaps just relevant to the Cloud Service Profile" — i.e. the spec currently conflates *the wire
contract every Benzene implementation must honour* with *conventions of particular optional
middleware*. That is the real issue behind 3a–3f, and it is a good catch: a porting author reading
`wire-contracts.md` today cannot tell which rows are mandatory.

**Suggested organising principle to test:** every header row should be labelled with its tier —
**(A) core wire contract** (required for interop), **(B) Cloud Service Profile** (required to be a
conformant Cloud Service), **(C) optional add-on** (only meaningful if you wired that middleware),
**(D) transport-binding detail** (belongs in `transport-bindings.md`).

### 3a. `traceparent` / `tracestate`

**Maintainer:** "W3C concept, nothing to do with Benzene directly, only relevant to Benzene if there
is tracing — and that is optional."

**Current state:** the spec calls these "Benzene's cross-service correlation contract"
(`wire-contracts.md` §2) — a strong claim for something Benzene neither defines nor requires.

**Assessment:** agreed, with a caveat. Benzene doesn't own W3C Trace Context and shouldn't restate
it. But it isn't purely optional either — *if* an implementation propagates trace context, doing it
verbatim per W3C is what makes cross-language mesh traces line up, and the mesh product depends on
that. So the likely landing: **tier C (optional)**, worded as "if you propagate trace context, do it
this way, verbatim per W3C" — a conditional conformance rule, not a Benzene contract. Drop the
"Benzene's correlation contract" phrasing.

### 3b. `x-correlation-id` → a self-contained correlation add-on

**Maintainer:** "purely for one implementation of correlation ID, more of an add-on. That
correlation add-on should just read it off the header, pass it to any clients, put it on the header
— all in one bit of configuration."

**Current state:** outbound-only; the spec already notes the inbound pickup middleware was removed
pre-1.0 and that honouring a partner's header is application middleware, not framework contract.
So the spec half-agrees already.

**Assessment:** two separable pieces.
- **Spec:** demote to tier C. It is an add-on convention, not a wire contract.
- **Product:** the maintainer is describing a *packaging* improvement — one switch that wires the
  whole correlation loop (read inbound header → carry through the scope → attach to every outbound
  client → write on the response). Today that is assembled from parts, and the inbound half was
  deliberately removed. Worth its own design note: it is a DX win and a very natural "one line to
  turn on" feature, but it reintroduces something previously cut, so the reasons for that removal
  need re-reading before reversing it.

### 3c. `topic` — keep, but make the naming consistent

**Maintainer:** "The topic is essential to message handlers, so that could definitely stay. Then we
want to look at whether we move the topic into Benzene headers, so we're consistent — or leave it as
`topic`, or give the users a choice. But whatever it is, it has to be used everywhere."

**Current state:** the header is bare `topic` (queue transports), while other Benzene-owned headers
are `benzene-`prefixed (`benzene-status`, `benzene-version`), and EventBridge uses a
`_benzeneHeaders` envelope object. Three different naming conventions for one family of concepts.

**Assessment:** the inconsistency is real and worth fixing before 1.0. The "give users a choice"
option should be **resisted** — a configurable wire-format key is an interop hazard (two conformant
services that can't talk to each other), and it multiplies the conformance matrix. Recommend: one
fixed name, chosen under §5's principle. Note the collision argument cuts the same way as item 4 —
a bare `topic` attribute on a shared SQS queue is far more likely to clash with an application's own
attribute than `benzene-topic` is.

### 3d. `benzene-status`

**Maintainer:** "looks like outbound gRPC, so it is only for gRPC. I'm not sure that can be classed
strictly as part of the Benzene spec."

**Current state:** `wire-contracts.md` §4.2 — a gRPC **trailer** carrying the raw Benzene status,
because several statuses collapse onto one gRPC code.

**Assessment:** agreed on placement, disagree on deletion. It exists to preserve fidelity the gRPC
status enum cannot carry, which is a genuine binding concern — so it belongs in
**`transport-bindings.md` (tier D)**, next to the gRPC mapping it serves, not in the general header
table where it reads as universal. Moving it is a doc change with no code impact.

### 3e. `content-type`

**Maintainer:** "content type and version, I guess, do kind of live there, but we should review
those."

**Assessment:** lowest-risk item. `content-type` is a genuine cross-transport need (transports
without a native slot). Likely outcome: keep, tier A/B, no change. Review for wording only.

### 3f. `benzene-version` → just `version`?

**Maintainer:** "whether `benzene-version` is sensible or whether we should just call it `version`."

**Current state:** marked **"Draft, not yet implemented"** in the spec.

**Assessment:** *this one is free right now and won't be later.* Because nothing implements it, the
name can be changed at zero cost — but only until someone ships it. Note the argument here runs
**opposite** to 3c/4: a bare `version` is *more* likely to collide with an application's own header
than `benzene-version`, and "version of what?" is genuinely ambiguous (payload schema? service?
protocol?). Provisional lean: keep a prefix, and consider `benzene-payload-version` or similar for
precision — but decide it under §5 with the others.

---

## 4. Prefix every reserved topic with `benzene`

**Maintainer:** "All of these topics like `mesh` — I think they should all be prefixed with
`benzene`. So anything that is strictly Benzene-related should start with `benzene`, so it has less
chance of clashing."

**Current state** (`src/Benzene.Schema.OpenApi/ReservedTopics.cs`): `spec`, `test-payloads`,
`healthcheck`, `liveness`, `readiness`, `mesh`, `invoke`, `report` — all unprefixed, all
extremely generic. Plus the mesh's own `mesh:*` family (`mesh:register`, `mesh:heartbeat`,
`mesh:traces`, `mesh:issues`, `mesh:query:*`) and the transport health probe topic `ping`.

**Assessment:** strongly agree, and the collision risk is not hypothetical — `report`, `invoke`,
`spec` and `ping` are words an application could plausibly want. This was already flagged as a
deferred task during the mesh work ("ultimately all benzene traffic should be prefixed 'benzene' …
will make filtering simple") and it has since acquired a **second** justification: the mesh UI's
utility-traffic filter currently maintains a hardcoded literal list (`isUtilityTraffic` in
`mesh-ui.html`) precisely because the names are unpredictable. A uniform prefix collapses that list
to a single prefix test — the UI code already carries a comment saying so.

**This is the biggest item on the list.** Blast radius: the reserved-topic constants, every mesh
wire topic, the collector's handler registrations, the conformance fixtures
(`docs/specification/conformance/*.json`), the Go port, the mesh UI filter, the example services,
and every deployed service's declared contract. It needs its own migration plan, including whether
old ids are accepted for a transition period or it is a clean break at 1.0 (a clean break is far
cheaper *now* than any transition mechanism later).

**Open sub-question:** separator. `benzene:mesh:register` vs `benzene.mesh.register` vs
`benzene-mesh-register` — the existing family uses `:` as a namespace separator, which argues for
`benzene:` as a prefix, giving `benzene:mesh:register`, `benzene:healthcheck`, `benzene:spec`.
Decide with §5.

---

## 5. The one decision underneath 3c, 3f and 4: the Benzene naming principle

Rather than three separate naming debates, settle **one principle** and apply it:

> Everything Benzene owns on the wire — reserved topic ids and framework headers — carries an
> unambiguous `benzene` marker, using one separator convention, with no configurable alternatives.

Then: topics become `benzene:*`, headers stay/become `benzene-*` (so `benzene-topic`,
`benzene-version`, `benzene-status`), and `_benzeneHeaders` is reviewed for consistency with that
family. The user-facing cost is one migration; the benefit is that "is this Benzene's?" becomes
answerable by inspection, forever — which is exactly the property the mesh UI filter, the reserved
topic list, and collision-avoidance all separately want.

**Counter-argument to weigh:** verbosity, and churn against a spec other people may already have
read. Neither looks decisive pre-1.0, but the ruling should say so explicitly rather than ignore it.

---

## Suggested order of work

1. **§5 naming principle** — unblocks 3c, 3f, 4; a decision, not an implementation.
2. **Item 2 (error payload)** — the largest genuine contract improvement; needs investigation first.
3. **Item 4 (topic rename)** — biggest blast radius, wants the longest runway before the tag.
4. **Items 3a/3b/3d/3e (header tiering)** — mostly doc restructuring; cheap, high clarity gain.
5. **Item 1 (CancellationToken)** — design note; additive, so it can land just after if needed.

Nothing here is decided. Each item should come back as a proposal with a recommendation, the
conformance/fixture impact, and the migration story where one is needed.
