# Benzene Clients Vision

**Status:** Living reference document
**Last Updated:** 2026-07-12
**Purpose:** Capture the aims for Benzene's outbound "pipes and adapters out" —
everything in `Benzene.Clients`, `Benzene.Clients.Aws`, `Benzene.Client.Http`, and
sibling packages — so a redesign of that layer is driven by a clear statement of intent
rather than by patching individual symptoms. Companion to
[`benzene-vision.md`](benzene-vision.md), which covers the framework as a whole; this
document narrows in on the one area that vision calls out as underdeveloped.

**Source:** Synthesized from a design conversation on 2026-07-12, prompted by an
architecture audit of `Benzene.Clients` done while investigating a DI bug in
`Extensions.AddLambdaClients` (see `work/aws-roadmap-1.0.md`, 2026-07-12 changelog).

---

## 1. The Problem: Benzene Has No Vision for the Way Out

[`benzene-vision.md`](benzene-vision.md) describes Benzene's inbound model precisely:
a native event (HTTP request, SQS message, SNS notification, Kafka record) is converted
by a thin adapter into a universal message (topic + body), a router matches the topic
to a registered handler by reflection, and the handler runs with zero knowledge of
which transport it arrived on. That model is coherent, well-tested, and consistently
applied — the audit that prompted this document confirmed the low-level client
implementations (`SqsBenzeneMessageClient`, `SnsBenzeneMessageClient`,
`AwsLambdaBenzeneMessageClient`, and their siblings) are just as clean.

The layer *above* those clients — the part responsible for deciding which one to use
and how to compose cross-cutting behavior onto them — has no equivalent coherent model.
Instead, three or four partial mechanisms have accreted, each solving a slice of "route
an outbound message to the right transport and decorate it" without any of them being
*the* answer:

- **Decoration done two unrelated ways.** `RetryBenzeneMessageClient` gets hand-nested
  around a client in constructor code in some places; `ClientBuilder.WithRetry()` →
  `IDependencyWrapper<T>` → `DependencyWrapperFactory` is a second, parallel decorator
  system for the identical concern. `Extensions.AddLambdaClients`'s DI bug (a client
  registered via bare `AddScoped<T>()` that can never resolve its constructor
  arguments) is a direct symptom of composing decorators by hand instead of through a
  single enforced mechanism.
- **Registration split by cardinality.** `SingleClientsBuilder` (one client) and
  `ClientsBuilder` (many, keyed by topic/service) are separate APIs with separate
  shapes, even though "one client" is just the N=1 case of "many clients."
- **Routing exists in three unrelated forms.** `IBenzeneMessageClientFactory.Create(service,
  topic)` is string-keyed, but every AWS factory ignores both arguments since it only
  ever has one client to return — the interface is sized for a case almost nothing
  uses. `BenzeneMessageClientFactory`'s matcher over `(service, topic)` pairs has three
  branches of specificity, is untested, and throws a generic `InvalidOperationException`
  at runtime with no startup-time validation that every topic a handler might send on
  actually has somewhere to go. `IClientMessageRouter.GetClient<TRequest>()` is a third,
  type-keyed routing concept, unrelated to the other two.
- **Ambient mutable header state.** `IClientHeaders` is a `Set`/`Get` dictionary
  injected into `HeadersBenzeneMessageClient`, rather than something threaded explicitly
  per send — a shape prone to leaking state across requests if it's ever registered
  with too wide a DI lifetime.

None of this is what a developer sending a message actually wants. Today, sending a
message means resolving a *specific* client type or navigating this factory/builder
machinery — which means the call site knows, one way or another, which transport it's
using. The inbound half of hexagonal Benzene achieved transport-unawareness. The
outbound half never did.

This isn't purely a greenfield gap, either. `Benzene.CodeGen.Client` already generates
a typed `{Service}ServiceClient` from a service's published spec (topics plus JSON
schema for request/response — served via the `UseSpec` middleware described in
`docs/spec.md`, in AsyncAPI, OpenAPI, or Benzene's own format), with one method per
topic and the method name already decoupled from the topic string via a pluggable
`IMethodName` strategy (`DefaultMethodName` derives it from the request payload type,
e.g. a `CreateOrderMessage` schema yields `CreateOrderAsync`; `TopicMethodName`/
`TopicReversedMethodName` derive it from the topic's colon-separated segments). That
generator is real, working code, and it already solves the specific problem of
decoupling a caller's method name from a callee's topic string. But every generated
method's body still calls `_clientFactory.Create(serviceName, topic)` — the exact
`IBenzeneMessageClientFactory` mechanism named above as one of the three unrelated
routing forms. The generated SDK is a good compile-time-safe surface sitting on top of
a foundation that needs the cleanup described in this document; fixing the foundation
should not mean throwing the generator away; it means giving it a single clean
mechanism to generate calls against instead of the current one.

---

## 2. Core Design Principle

### 2.1 Outbound sending is topic-routed through middleware pipelines, exactly like inbound handling — just running the other way

This is the single idea this document exists to state:

> A developer's business logic should be able to say "send this message on this topic"
> and be done — with no idea, and no need to know, whether that topic ends up on an SNS
> topic, an SQS queue, an HTTP call, a Lambda invocation, or a Step Functions execution.
> That mapping is configuration, resolved once at startup, not something the call site
> participates in.

Benzene already has the right *pipe* primitive for this: `SqsClientMiddleware`,
`SnsClientMiddleware`, and `HttpClientMiddleware` are ordinary middleware, the same
shape used everywhere else in the framework. What's missing is the routing layer above
them — the outbound mirror of `MessageRouter` and its reflection-driven
`[Message("topic")]` handler discovery. Inbound, the router is built for you by
convention. Outbound, there is no way to *discover* that "topic x should go to SNS
topic y" — that binding is inherently a piece of deployment configuration, not
something reflection can infer — so the outbound router's table has to be configured
explicitly. But it should still be exactly one router, one place, one topic-keyed
lookup, with retry/headers/logging/etc. living as ordinary pipeline middleware rather
than a bespoke decorator system layered outside it.

**Design-decision test:** does a piece of business logic that sends a message need to
name a transport-specific type (`SqsBenzeneMessageClient`, `IAmazonSNS`, etc.) to do
so? If yes, the transport has leaked into the core, the same failure mode
[`benzene-vision.md` §2.1](benzene-vision.md#21-a-service-is-defined-by-what-it-does-not-by-its-transport)
already rules out for the inbound side.

### 2.2 One routing table, configured once, validated at startup

The outbound routing table — "topic → transport pipeline" — should be a single,
explicit, developer-authored mapping, analogous to how the inbound side's implicit
mapping ("topic → handler") is built by `UseMessageHandlers()`. Because it can't be
discovered by reflection, it deserves better startup-time guarantees than what exists
today: a message handler that sends on a topic with no configured destination should be
a caught misconfiguration, ideally at DI-container-build time, not a runtime
`InvalidOperationException` the first time that code path executes in production.

**Design-decision test:** if a topic's outbound destination is missing or
misconfigured, is that caught before the first real message tries to use it, or does it
surface as a production incident?

### 2.3 Decoration is middleware, not a second decorator system

Retry, header injection, logging, and any future cross-cutting outbound concern belong
in the same middleware-pipeline vocabulary Benzene already uses for everything else —
see [`benzene-vision.md` §2.4](benzene-vision.md#24-one-middleware-pipeline-shared-by-every-adapter).
There is no principled reason for outbound decoration to have invented its own parallel
`IDependencyWrapper<T>`/`DependencyWrapperFactory` mechanism alongside manually-nested
constructor wrapping — that duplication is pure accretion, not a deliberate design
choice, and it's exactly the kind of drift the shared-pipeline principle exists to
prevent.

**Design-decision test:** is a new cross-cutting outbound concern being added as
pipeline middleware (reusable, composable, ordered, testable in isolation), or as
another bespoke wrapper class? If the latter, it's repeating the mistake this document
is naming.

### 2.4 Cardinality is not an API decision

Whether a service sends on one topic or fifty should not determine which builder API a
developer reaches for. `SingleClientsBuilder` existing as a separate concept from
`ClientsBuilder` is evidence the current design treats "how many destinations" as an
architectural fork, when it's really just how many rows are in the routing table from
§2.2.

**Design-decision test:** does a design distinguish "one client" from "many clients" as
different code paths, or does it treat "one" as the trivial case of "many"?

### 2.5 Generated, spec-driven clients are the primary developer-facing surface — topic strings are an implementation detail, not an API

`sender.SendAsync(topic, message)` (§2.1) is already an improvement over resolving a
transport-specific client by hand, but it still couples caller and callee through a
hand-typed topic string — the callee renaming `"order:create"` to `"orders:create"` is
a silent runtime break for every caller, not a compile error. That's the problem
`Benzene.CodeGen.Client` already exists to solve: generate a typed client
(`OrderServiceClient.CreateOrderAsync(payload)`) from the target service's published
spec, so the topic string lives exactly once — inside generated code owned by the
calling service's build, regenerated whenever the target's spec changes. A rename on
the far side then does one of two things, both better than a silent runtime miss: the
method-naming strategy picks it up automatically (if it derives the name from the
payload type, per `DefaultMethodName`, and the payload type didn't change), or the
generated method disappears/renames and every call site fails to compile until updated.

This makes the raw `IBenzeneMessageClient`/`sender.SendAsync(topic, ...)` surface from
§2.1 the *low-level* mechanism — necessary as a foundation, and the escape hatch for
dynamic or infrastructure-level sends, but not what application code calling a known
service should be reaching for day to day. The generated client is the intended
default; hand-typed topic strings at a business-logic call site are the exception that
needs justifying, the same way `Benzene.Aws.Lambda.Core` treats a raw
`AwsLambdaMiddlewareRouter` as the low-level escape hatch beneath `AwsLambdaStartUp`.

**Design-decision test:** if the microservice on the other end of a topic renames it,
does every caller with a generated client fail to build (or silently keep working via
payload-type-derived naming), or does the failure only surface at runtime the first
time a caller happens to exercise that code path? A design that can't offer the former
hasn't finished the job §2.1-§2.4 started.

---

## 3. What This Vision Optimizes For (and What It Deliberately Doesn't)

**Optimizes for:**
- Business logic that sends a message by topic alone, with zero transport knowledge —
  the exact mirror of how it *receives* a message by topic alone
- Generated, spec-driven clients as the default way one Benzene service calls another —
  a topic rename on the far side becomes a build break or an automatic pickup, not a
  runtime surprise
- A single, coherent routing/decoration model shared with the rest of the framework
  (middleware pipelines), rather than a client-specific parallel universe of builders
- Misconfigured outbound routing failing loudly at startup, not silently in production
- Retry, headers, and future cross-cutting concerns being ordinary, composable
  middleware — testable and reasoned about the same way inbound middleware already is

**Deliberately does not optimize for:**
- Reflection-based *discovery* of outbound routing the way inbound handler discovery
  works — the topic-to-transport binding is inherently external configuration
  (which SNS topic, which queue, which endpoint) and pretending otherwise would trade
  a real problem for a worse one (magic, undiscoverable routing)
- Preserving every existing class in `Benzene.Clients` as-is — this document is
  explicitly the "aims" step that precedes and justifies removing or consolidating
  mechanisms found to be redundant, per the audit in §1

---

## 4. Using This Document

This document intentionally stops at principles — it does not prescribe interface
names, method signatures, or a migration plan. The next step is a design pass that
proposes a concrete shape for the outbound router and pipeline, checked against
section 2 before being checked against any existing code:

1. Does it let business logic send by topic alone, with no transport-specific type at
   the call site?
2. Is the topic-to-transport binding one explicit, startup-validated table — not
   several partial, string-keyed, or type-keyed lookup mechanisms coexisting?
3. Is decoration (retry, headers, anything else) expressed as middleware in the same
   vocabulary as the rest of Benzene, not a bespoke wrapper system?
4. Does the design avoid treating single-destination and multi-destination services as
   architecturally different cases?
5. Can `Benzene.CodeGen.Client` generate a compile-time-safe client against it, such
   that a topic rename on the far side surfaces as a build break rather than a runtime
   miss?

A proposed design that fails any of these should be revised before it's implemented,
not accepted as a pragmatic compromise — this area already has enough of those.

---

## 5. Related Documents

- [`benzene-vision.md`](benzene-vision.md) — the framework-wide vision this document
  narrows in on; §2.1, §2.3, and §2.4 of that document are the inbound-side statements
  this document mirrors for the outbound side
- [`docs/spec.md`](../docs/spec.md) — the `UseSpec` middleware that publishes a
  service's topics and JSON schemas (AsyncAPI/OpenAPI/Benzene format), which
  `Benzene.CodeGen.Client` consumes to generate the typed clients described in §2.5
- [`docs/client-sdks.md`](../docs/client-sdks.md) — **2026-07-14 correction:** this file now
  exists (added by commit `9d01774`, 2026-07-13 21:43 UTC, before this document was even written)
  and `docs/index.md` no longer marks it "Coming Soon" — the note below describing it as
  not-yet-written was already stale at authoring time. It documents `Benzene.CodeGen.Client`'s
  generated-client story for end users, separate from this internal vision document; still worth
  revisiting once/if §2.5's outbound-routing redesign lands, since the generated client's method
  bodies still call the `IBenzeneMessageClientFactory` mechanism this document argues needs
  replacing (see §1 above)
- [`work/aws-roadmap-1.0.md`](aws-roadmap-1.0.md) — records the `AddLambdaClients` DI
  bug (2026-07-12 changelog) that prompted the architecture audit behind this document
