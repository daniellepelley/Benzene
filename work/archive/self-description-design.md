# One self-description — design spike

> ARCHIVED 2026-10-11 — actioned. Evidence: the spec release `spec-1.0.0` (Benzene `5dd10f17`); `docs/specification/contract-document.md` and `mesh.md` §1–§2 (spec-0.3.0, spec-0.4.0).

**Status:** DESIGN — **ruled 2026-10-11** (§10.5); Phase 0.2 of
[`spec-simplification-plan.md`](spec-simplification-plan.md).
**Written:** 2026-10-11. **Actions:** decision D18, and the review it comes from
([`spec-review-2026-10.md`](spec-review-2026-10.md) §2.1, §4.9, §8).
**Method:** the code was read, not the docs: benzene-dotnet, benzene-go, benzene-typescript,
benzene-python and benzene-ui at their current checkouts, and the conformance fixtures here. The
worked-example hashes in §4 were computed with a canonicaliser equivalent to RFC 8785 for the inputs
used (ASCII, no fractional numbers). It reproduces all four values in `contract-hash-cases.json`
exactly.
**Shape:** the seven questions of plan 0.2. For each: the options considered, the recommendation,
why, the cost per port and the consumers affected. §9 lists what the maintainer has to rule on.

---

## 0. Recommendations at a glance

| # | Question | Recommendation | Departs from D18 / plan text? |
|---|---|---|---|
| 1 | `events[]` / `sends[]` | Two lists, distinguished by **who owns the contract**, not by whether a reply comes back. `events[] {topic, version?, message, example?}`; `sends[] {topic, version?, request, response?}`, where an absent `response` means one-way. Every port's outbound registration needs an explicit event/send kind. The collector graph's providers are **events ∪ sends**. | Yes: the plan says "the collector's graph reads `sends` for providers". It has to read both. |
| 2 | Hash projections | One algorithm over an **allow-listed, dereferenced** projection. `contractHash` = H({requests, events}); `registrationHash` = H({requests, events, sends}). Identity, placement, runtime, `serviceVersion`, `info`, endpoints, transports, examples, schema annotations, component names and entry order are all outside it. Heartbeats carry `registrationHash` and gain `serviceVersion`. The document carries both hashes. | Partly: "requests + events + components" becomes "requests + events, with components inlined". |
| 3 | Component naming | Names stop being contract: the hash dereferences, so naming and hoisting depth cannot break cross-port equality. The rule stays normative for validity, uniqueness (a collision fails the build), determinism, a declared-name escape hatch, and the default name (declared name, else the simple type name). | Yes: "a normative component-naming rule" becomes "normative, but not load-bearing for the hash". |
| 4 | `{type, format}` | Optional request; absent, empty or `{}` means `benzene`/`json`. An unsupported value gets `not-implemented`, not a silent default. `GET /benzene/spec?type=&format=` copies the query onto the request. Only the default output is conformance-tested. | No |
| 5 | Fields | `openapi` → `schemaDialect: "openapi-3.0"`. `info` → `service` / `serviceVersion` / `description`. `reserved` and reserved entries are **dropped**. `example` stays optional, author-supplied only. `messageEndpoint` and `transports` are kept, and `transports` absorbs `binding`. `instanceId` leaves the document. `versionScheme` is new on the wire. | No (these are the decisions 0.2 asked for) |
| 6 | Migration | The derivation rule (mesh §2.1, rewritten for OpenAPI 3.0) and a new `derivationCases` group land in **3a**, not 3b. 3b is then only identity fields, the register body and the collector fixtures. benzene-ui's work is all in 3a: it never reads the register body. | Yes: it moves the §2.1 rule from 3b to 3a, and benzene-ui's position in 3b. |
| 7 | Risks | .NET's schema generator (Swashbuckle) has to give way to the derivation rule. Go has no contract-document producer at all. Register-body size. `sends` visible on a public `/benzene/spec`. | — |

---

## 1. What the code does today

### 1.1 Survey

| | .NET | Go | TypeScript | Python |
|---|---|---|---|---|
| Contract-document producer | `EventServiceDocumentBuilder.cs` + Swashbuckle `SchemaBuilder.cs` | **none**. `mesh.SpecHandler` serves the *ServiceDescriptor* at `/benzene/spec` (`mesh/spechandler.go`) | `EventServiceDocumentBuilder.ts`, schemas from validators (`SchemaBuilder.ts`) | `core/contract.py` `ContractDocument.derive` |
| `benzene:spec` topic | handler, `SpecRequest {type, format}`, defaults to `benzene`, **any unknown type → benzene** (`SpecBuilder.cs`) | none | as .NET; `format` ignored (`SpecBuilder.ts`) | interception answering the **native** `{service, topics}` (`core/spec.py` `spec_interception`); HTTP serves the contract document, `?type=native` the native shape (`http/app.py`) |
| `events[]` fed from | `ResponseEventCatalog` only at run time. `SpecBuilder.CreateSpec` never calls `AddMessageSenderDefinitions` | — | broadcast **and sender** definitions | **every** `produces` declaration |
| Descriptor `produces` from | `MeshOutboundRegistry` | `mesh.OutboundRegistry` | `IMessageSendersFinder` | `mesh.OutboundRegistry` |
| Event vs. send at registration | not distinguishable: `Register<T>` uses a `Void` response for both | not distinguishable: `RegisterOutbound[T, any]` for both | partly (broadcast vs. sender definitions) | not distinguishable |
| Component name | CLR `Type.Name`; generic → `{Arg}{Def}` (Swashbuckle 6.5 default id selector) | no components: schemas are inline (`openapi/openapi.go`: "the descriptor carries no Go type name") | constructor `name`; anonymous class → inline | dataclass `__name__` |
| Hoisting depth | every complex type, nested ones included, and enums | — | top-level payload only (one validator schema per type) | top-level dataclass only, nested ones inline |
| Name collision | throws (Swashbuckle `SchemaRepository`) | — | **first registration wins, silently** (`addNamedSchema`) | **suffixes `2`, `3` in registration order** (`_Catalogue._unique`) |
| Schema conventions | `nullable: true`, `format: double`, `additionalProperties: false` on every object, validation rules as `description` text (committed `examples/AwsMesh/Orders/contracts/payments.spec.json`) | §2.1 subset, `["T","null"]` | the validator's JSON Schema | §2.1 subset, cycle cut with `{}` (`core/schema.py`) |
| Entry order | registration order | — | registration order | sorted by (topic, version) |
| `example` | generated (`ExamplePayloadBuilder`) | — | not ported (`RequestResponse.ts`) | none |
| Provider-side drift hash | `SchemaHealthCheck` = `ContractHash.Compute(handlers)` | `clienthealthcheck` compares the provider's **`descriptorHash`** with the expected value | `SchemaHealthCheck.ts` = `MeshHashing.computeHash`, an HMAC over the raw JSON | none |
| Service-level client components | **narrowed** to the requests' closure before hashing (`MessageClientSdkBuilder.NarrowToReachableSchemas`, #170) | not narrowed (§5.3) | not narrowed | not narrowed (its docstring says this "matches .NET") |
| `versionScheme` on the wire | no: validated, then discarded (`Benzene.Descriptor/Program.cs` "[DECISION] round 11") | no | no | no |
| `profile` on the descriptor | yes | **no** | yes | yes |

### 1.2 Findings that shape the design

- **F1. Go serves no contract document.** `GET /benzene/spec` returns the descriptor, and Go has
  no `benzene:spec` topic. Profile R5 says "the served document's shape is contract-document.md", but
  `docs/capabilities.md` marks Go "yes" for R5. Go's Phase 3 item is a whole new producer (naming,
  OpenAPI 3.0 dialect, JCS on the service side), not a re-cut of one. The plan's "the four contract
  generators" (3a) overstates what exists.
- **F2. Sends are already folded into `events`, deliberately.** The review attributes this to
  .NET's builder. In fact the builder's `AddMessageSenderDefinitions` is dead at run time. The
  folding happens in TypeScript's `SpecBuilder.ts`, in Python's `ContractDocument.derive`, and in
  .NET *applications*: `examples/AwsMesh/Shared/MeshServiceWiring.cs` declares every send as a
  response event "→ the mesh's structural topology edge". The committed
  `Orders/contracts/payments.spec.json` therefore lists the command `shipping:book` under `events`.
  The reason is that the pull-mode aggregator builds `producers` from `events`
  (`MeshAggregator.BuildTopicCatalog`), and so does Python's poller (`mesh/specdoc.py`
  `spec_produces`). Splitting the lists means both readers take events ∪ sends.
- **F3. The response type does not separate events from sends.** In Go's
  `examples/azure-functions-mesh/domain/domain.go`, `payment:take` (a one-way Service Bus command) and
  `order:placed` (an Event Hub fan-out event) are registered identically: `RegisterOutbound[T, any]`.
  The kind has to be declared.
- **F4. The drift check does not work in three ports.** In .NET, the provider hashes its handlers
  with empty `info`, no events, and components that include schemas only reserved topics use
  (`SpecRequest`, `RawStringMessage`). The generated client hashes the served document, which has
  `info` filled in, events, and components narrowed to the requests. The two agree only in the test's
  contrived case (`ContractHashProviderClientAlignmentTest`). TypeScript's provider uses the old
  HMAC. Go compares `descriptorHash` with a `contractHash`. Every one of these is a mismatch between
  projections.
- **F5. `contractHash` depends on things that are not contract.** JCS sorts object members, not
  arrays, and .NET and TypeScript emit `requests` in registration order. `info` (title, description,
  application version) is hashed "as it stands" (contract-document §6.2), so a description edit, or
  any new release, changes every client's expected hash. And because `components` is hashed with its
  names, renaming a DTO counts as drift, as does a port that hoists differently.
- **F6. Python already dereferences, for exactly this reason.** `specdoc._resolve` inlines `$ref`s
  before the collector stores schemas, because "a reference into a catalogue the collector never sees
  would compare as 'the shape changed' the moment a producer renamed a class."
- **F7. `versionScheme` is required but has no field.** service-versions §2 says a declared `serviceVersion`
  MUST carry a `versionScheme`. §2's shape has no such field and no port emits one. 3b adds it to the
  wire; nothing moves.
- **F8. benzene-ui never reads the descriptor.** It reads the aggregator's artifacts (`topics.json`
  `producers`, which come from contract-document `events`), the contract document itself (Spec page),
  and `benzene:mesh:query:fleet` (`src/data/meshApi.ts`). The review (§4.13) and the plan's 3b say
  "benzene-ui reads `produces`". It reads the selector-computed `produces`, built from the
  aggregator's `producers`. So benzene-ui moves in 3a, and 3b barely touches it. Its vendored
  `contracts/mesh-descriptor-cases.json` is a stale (`consumes`) copy that nothing imports.
- **F9. The Count is unaffected.** It pins none of `Benzene.Schema.OpenApi`, `Benzene.Mesh.*`,
  `Benzene.CodeGen.*` or `Benzene.CloudService`, so the canary run for Phase 3 is a formality.
- **F10. Two service names in .NET.** The document's `info.title` comes from `IApplicationInfo.Name`
  and the descriptor's `service` from `MeshServiceInfo.Service`. They are configured separately and
  can differ. Merging the documents forces one of them to win.

---

## 2. `events[]` and `sends[]`

### 2.1 What separates them

Not the transport, and not whether a reply comes back (F3). **Ownership of the contract:**

- an **event** is a topic this service *publishes and defines*. Any party may subscribe, and the
  payload schema is this service's to change. It is part of what a client of this service depends on.
- a **send** is a topic this service *sends to*, whose contract **another party defines**: a command
  to a queue, an RPC, or a publish onto someone else's topic. It is a dependency of this service and
  nothing a client of it can observe.

So `payment:take` is a send of `orders` (payments owns it), `order:placed` is an event of `orders`,
and both are one-way.

### 2.2 Options

| | Option | Verdict |
|---|---|---|
| A | One `outbound[]` list with `kind: "event" \| "send"` | One shape, but every reader filters by kind and the two hash projections have to filter inside a list. Rejected: no simpler for anyone. |
| B | Two lists, `events[]` and `sends[]` (D18) | **Recommended.** Each projection is a set of top-level members, so the hash allow-list (§3) is trivial. |
| C | Infer the kind: a declared response means a send, otherwise an event | Rejected by F3: one-way commands would be misfiled as events, and adding a dependency would change `contractHash`. That false drift is exactly what the review wants removed. |

### 2.3 Shapes (recommended)

```json
"events": [ { "topic": "order:placed", "version": "v1",
              "message": { "$ref": "#/components/schemas/OrderPlaced" } } ],
"sends":  [ { "topic": "payment:take",
              "request": { "$ref": "#/components/schemas/TakePayment" } },
            { "topic": "inventory:reserve", "version": "v2",
              "request":  { "$ref": "#/components/schemas/Reserve" },
              "response": { "$ref": "#/components/schemas/Reservation" } } ]
```

- `events[]`: unchanged from contract-document §3 (`topic`, `version?`, `message`, `example?`).
- `sends[]`: `topic`, `version?` (the version the sender writes into `benzene-version`), `request`,
  `response?`. **An absent `response` means one-way** (the send is acknowledged, no payload comes
  back). `response: {}` means a reply is expected but its shape is unconstrained. This separates the
  two meanings Go currently folds into `any`. The field names mirror `requests[]`, because a send is
  the client-side image of someone else's request. There is no `example` and no `httpMappings`.
- A (topic, version) MUST NOT appear in both `events` and `sends`; deriving a document that does so
  is a start-up error. A topic MAY appear in `requests` as well as in either list (a service that
  forwards).
- Both lists come from **explicit registration**. mesh §2.3's rules (no inference from call sites,
  no destination addresses, an undeclared send surfaces as `contract-drift`) move to
  contract-document unchanged, now covering both lists.
- A port that cannot derive one of them says so in `degraded` (§6). It never emits an empty array
  in that case.
- **Graph (3b):** providers = `events` ∪ `sends`; consumers = `requests`. mesh §4.2's "undeclared"
  becomes "absent from the caller's `events` ∪ `sends`". The plan's sentence "the collector's graph
  reads `sends` for providers" has to say both, or a published event has no provider edge.

### 2.4 Mapping each port's registration

| Port | Today | Becomes | API change |
|---|---|---|---|
| .NET | `ResponseEventCatalog` (`AddResponseEventDeclarations`) → `events`; `MeshOutboundRegistry.Register<TReq[,TRes]>` → `produces` | response events → `events`; `MeshOutboundRegistry` → `sends`; add `MeshOutboundRegistry.Publish<T>(topic)` for published events that are not response events | `AddMessageSenderDefinitions` writes `sends`, and `SpecBuilder` feeds it. `examples/AwsMesh` `OutboundSend` splits into commands and events. |
| Go | `RegisterOutbound[TReq,TRes]` → `produces`; `asyncapi`/`openapi.WithSentEvent(topic, map)` → AsyncAPI only | `RegisterOutbound` → `sends` with `response`; add `RegisterSend[TReq]` (one-way) and `RegisterEvent[T]` → `events` | `WithSentEvent` reads the registry instead of a caller-built schema map |
| TypeScript | broadcast definitions + **sender definitions → `events`**; senders → `produces` | broadcast → `events`; senders → `sends` (they already carry `responseType`) | `EventServiceDocumentBuilder.addMessageSenderDefinitions` targets `sends` |
| Python | `produces=` → **`events`** (document) and `produces` (native, descriptor) | `OutboundRegistry.publish(topic, type)` → `events`; `.send(topic, request, response=None)` → `sends`; a bare topic string is a send with `request: {}` | `ContractDocument.derive(events=…, sends=…)`; `specdoc.spec_produces` reads events ∪ sends |

**Consumers:** .NET `MeshAggregator.BuildTopicCatalog` and TypeScript's port of it, plus Python's
poller (`specdoc.py`), read events ∪ sends for `producers`. That is 3a, since all three read the
contract document. With the split, the pull-mode graph gains real RPC dependencies; today it only has
whatever applications smuggled into `events`. benzene-ui's Spec page gains a "Sends" section (new),
and its summary counts sends separately from events. In .NET's AsyncAPI export, events become `send`
operations on owned channels and sends become `send` operations on foreign channels. That export is
not conformance-tested. **Cost:** .NET S, Go S (plus the producer work, §8), TypeScript S, Python S,
benzene-ui S.

---

## 3. The two hash projections

### 3.1 Options for what is hashed

| | Option | Problem |
|---|---|---|
| a | Today: the whole document minus a deny-list (`example`, `messageEndpoint`, `transports`, reserved entries) | `info`, entry order, component names and hoisting depth are all hashed (F5); every new top-level field needs a new strip rule |
| b | Allow-list the members; hash `components` narrowed to the closure of the projected entries | Fixes reserved-only and event-only components and the #170 divergence (F4), but a DTO rename, or a port that hoists one level deeper, still changes the hash |
| c | Allow-list the members and **dereference**: inline every `$ref`, so components are hashed through their uses | **Recommended.** Name-, hoisting- and order-independent by construction; nothing outside the allow-list can leak in |

### 3.2 The projection (normative text, proposed)

`hash(P) = "sha256:" + lowercase-hex(sha256(JCS(P)))`, where P is built from a document by:

1. **Members.** Take `requests` and `events` (for the registration projection, `sends` too) and
   nothing else. Every other top-level field (identity, hashes, `components`, `messageEndpoint`,
   `transports`, `profile`, `degraded`, and any future field) is outside P by construction.
2. **Entries.** Drop any entry whose topic starts with `benzene:` (a tolerance rule; producers do
   not emit such entries, §6). Keep only these members:
   `requests {topic, version?, httpMappings?, request, response}`, `events {topic, version?,
   message}`, `sends {topic, version?, request, response?}`.
3. **Dereference.** Replace each `{"$ref":"#/components/schemas/N"}` with N's schema, normalized by
   the same steps, recursively. A reference to a component already being expanded on the current
   path becomes `{}`. This is mesh §2.1's cycle cut, which all four ports already implement for the
   descriptor.
4. **Annotations.** At every depth, remove `title`, `description`, `example` and `x-*` members, and
   sort every `required` array (it is a set; .NET's `MeshSchemaGenerator` already sorts it, while
   Go, Python and the validators keep declaration order).
5. **Order.** Sort entries by (topic, then version, with an absent version first), and
   `httpMappings` by (method upper-cased, path), all ordinal.

Then:

- **`contractHash` = hash({requests, events})**: what a client of the service depends on.
- **`registrationHash` = hash({requests, events, sends})**: what the collector's graph depends on.
  `sends` is always present in this projection, empty or not, so the two values never coincide by
  accident.

### 3.3 Membership compared with today

| Member | `descriptorHash` today | `contractHash` today | `contractHash` | `registrationHash` | Why |
|---|---|---|---|---|---|
| requests / topics (+ schemas) | in | in | in | in | the contract |
| events | — | in | in | in | clients subscribe to them |
| sends / produces | in | — | **out** | in | a dependency is not client-visible (review §2.1) |
| `httpMappings` | — | in | in | in | HTTP is a Core binding; a route change reaches HTTP callers (ruling Q2) |
| `service`, `serviceVersion`, `versionScheme` | in | — (`info` was in) | out | out | a version is an entity, not a shape (service-versions §1); comparisons always happen inside one `(service, serviceVersion)` key |
| `info` | — | **in** | out (gone, §6) | out | F5 |
| `runtime`, `placement`, `binding`/`transports` | in, in, in | —, —, out | out | out | deployment facts: two regions of one release, under one key, would otherwise report `hashMatches: false` against each other |
| `instanceId`, `degraded`, `profile` | out | — | out | out | unchanged |
| `messageEndpoint`, `example` | — | out | out | out | unchanged |
| component names, hoisting depth | — | **in** | out | out | F5, F6, §4 |
| entry order, `required` order | in (sorted) | **in** | out | out | F5 |
| schema `title`/`description` | in | in | out | out | documentation; .NET puts validation prose there |

Two fixture properties are reversed on purpose: `mesh-descriptor-cases.json`'s
`sensitiveToServiceVersion: true`, and mesh §2.2's "MUST change when … `serviceVersion`, placement"
changes.

### 3.4 Where the hashes live

- **In the document.** `contractHash` and `registrationHash` are REQUIRED on every document a
  producer emits, whether served or registered. They are outside P (step 1), so carrying them costs
  no strip rule. A drift check compares against the served value; any tool MAY recompute it. This
  replaces each port's private provider-side hash (F4): .NET's and TypeScript's `SchemaHealthCheck`
  publish the document's `contractHash`, and Go's `clienthealthcheck` reads it from `benzene:spec`
  instead of reading `descriptorHash` from `benzene:mesh`.
- **On heartbeats.** `descriptorHash` is renamed `registrationHash`, and the heartbeat **gains
  `serviceVersion`**. mesh §5 already requires the comparison to be scoped to one
  `(service, serviceVersion)`, but the heartbeat shape has no field to scope it with, so .NET's
  collector matches against *any* registered version (`MeshCollectorStore.HashMatches`). Plan D26
  (fleet rows under versions) is adjacent but independent.
- **The register body** carries `registrationHash` (as above). The collector trusts it and MAY
  verify it. A collector that recomputes needs the dereference and JCS; none is required to.

### 3.5 Projection comparability (contract-document §6.4, extended)

| Projection | Members | Produced by | Comparable only with |
|---|---|---|---|
| whole-service contract | all domain `requests`, all `events` | the document's `contractHash`; a default service-level client | itself |
| registration | + all `sends` | the document's `registrationHash`; heartbeats | itself, within one `(service, serviceVersion)` |
| include-list client (§5.2) | the listed `requests`, all `events` | that client | the same include-list recomputed over the provider's current document |
| topic-scoped client (§5.3) | one `request`, `events: []` | that client | the same topic recomputed over the provider's current document |

Because components are dereferenced, the question "is a service-level client's `components`
narrowed or not?" no longer affects any hash. The .NET and Go/TypeScript/Python divergence (§1.1)
disappears without either side changing its generator. A default (no include-list) client's hash now
equals the served `contractHash`. That is the drift check F4 lacks today.

**Cost:** each port's `ContractHash`/`contractdoc.Hash`/`compute_contract_hash`/`computeContractHash`
gains dereferencing, annotation stripping and sorting (S each). Each port's announcer and heartbeat
switches field (S). Go additionally needs JCS on the service side: its root module is
dependency-free and `github.com/gowebpki/jcs` lives only in `codegen/go.mod` (ruling Q8).
**Consumers:** generated clients (every embedded hash changes once, pre-1.0; nothing in The Count is
affected, F9), health-check clients, collectors' `hashMatches`.

---

## 4. Component naming and hoisting

### 4.1 Options

| | Option | Cost | Verdict |
|---|---|---|---|
| A | Names derived from topic and role (`orders.create.Request`) | Generated DTOs become topic-shaped. A type shared by N topics becomes N components, so codegen emits N identical classes. Nested types need path names. All four ports rename everything users see. | Rejected: a DX regression every port's champion would block |
| B | A normative host-type rule (declared name, simple name, generics, *normative hoisting depth*) carrying hash equality | TypeScript cannot name nested validator shapes. Go's `reflect` names for generics (`Page[pkg.Order]`) need a canonical form. .NET would have to stop hoisting nested types, or the others start. A class rename is still a contract change. | Rejected: it pins host-language details to make a hash work |
| C | **Names are not contract.** The hash and the conformance comparison are name- and hoisting-independent (§3.2, step 3). The rule pins validity, uniqueness, determinism and a default name. | Each port keeps its naming and hoisting; collisions become loud everywhere; one declared-name hook per port | **Recommended** |

C narrows D18's "a normative component-naming rule". The rule is still normative, but it is no
longer load-bearing for cross-port equality. That needs a ruling (Q1). The precedent for "names are
API shape, not contract" is contract-document §5.5, which already excludes generated method names.

### 4.2 The rule (normative text, proposed)

1. **What a component is.** A component is the schema of a *named* object or enum type. A producer
   SHOULD hoist every named object type reachable from an entry, and MAY inline any schema. Arrays,
   maps and primitives are never components.
2. **Default name.** The type's declared schema name if one is declared; otherwise its simple name,
   with no namespace, module or package. For a generic instance, the type-argument names precede the
   definition name (`Page<Order>` → `OrderPage`), as Swashbuckle does by default (SHOULD).
3. **Validity (MUST).** Keys match `^[A-Za-z0-9._-]+$` (the OpenAPI 3.0 components key pattern);
   other characters are dropped.
4. **Uniqueness (MUST).** Two schemas that differ after normalization (§3.2, steps 3–4) MUST NOT
   share a name. Derivation fails at start-up or build time and names both types. They MAY share one
   entry when their normalized schemas are identical. Suffixing (Python) and first-wins
   (TypeScript) are both forbidden.
5. **Declared names (MUST).** A port offers a way to declare a payload type's schema name, as the
   remedy for rule 4. The mechanism is an idiom: an attribute or registration option in .NET, an
   optional `BenzeneSchemaName() string` method or registration option in Go, a name on the
   `ITypeJsonSchemaSource` mapping in TypeScript, a class attribute or registration option in Python.
6. **Determinism (MUST).** The same registry yields the same names on every build. Registration
   order must not influence a name (rule 4 already removes the one way it did).
7. **Recursion.** A recursive named type is hoisted and refers to itself by `$ref`. The projection
   cuts it at the cycle (§3.2, step 3).
8. `$ref` points only into `#/components/schemas/` (contract-document §4, unchanged).

**Conformance:** the canonical payload types get fixed names in `conformance/README.md`
(`GreetRequest`, `GreetResponse`, `Greeted`, `LogEntry`, `LookupRequest`, `LookupResponse`). Each
runner declares types with those names, so the canonical document matches across ports *including*
component keys. `derivationCases` (§7) still compares dereferenced schemas per entry, and asserts
that keys are valid and every `$ref` resolves, so a port that hoists differently is not failed for
it.

### 4.3 Worked examples

**Ex 1: the canonical service, .NET and Go.** .NET declares
`record GreetRequest([property: JsonPropertyName("name")] string Name)` and so on. Go declares
`type GreetRequest struct { Name string \`json:"name"\` }`. Both register `conformance:greet`, the
event `conformance:greeted` and the one-way send `conformance:log`, and both emit:

```json
"requests": [{ "topic": "conformance:greet",
               "request": { "$ref": "#/components/schemas/GreetRequest" },
               "response": { "$ref": "#/components/schemas/GreetResponse" } }],
"events":   [{ "topic": "conformance:greeted", "message": { "$ref": "#/components/schemas/Greeted" } }],
"sends":    [{ "topic": "conformance:log", "request": { "$ref": "#/components/schemas/LogEntry" } }],
"components": { "schemas": { "GreetRequest": { "type": "object",
    "properties": { "name": { "type": "string" } }, "required": ["name"] }, "…": {} } }
```

`contractHash` = `sha256:4f27f0fa…df58f475`, `registrationHash` = `sha256:ad70e74c…2e957958`. The
same document with every schema inline and `components` empty, which is what Go produces today,
gives **the same two values**. Under today's rule the two forms hash differently (`d1335274…` vs.
`6784000b…`). Emptying `sends` leaves `contractHash` unchanged and moves `registrationHash` to
`60a2d54a…`.

**Ex 2: hoisting depth, .NET and TypeScript.** `CreateOrder { customer: Customer; lines: LineItem[] }`.
.NET (Swashbuckle) hoists `CreateOrder`, `Customer` and `LineItem`. TypeScript's zod source yields
one schema for `CreateOrder` with `customer` and `lines[]` inline, and lists `required` in a
different order. Both give `contractHash` `sha256:674b753f…2f416020`. Today's rule gives
`986c3087…` and `406512f6…`, so the drift check would report drift between two ports describing the
same wire shape. Renaming `OrderDto` to `Order` also leaves `674b753f…` unchanged.

**Ex 3: a collision, Go and Python.** `orders.Item {sku}` and `payments.Item {paymentId}` are both
reachable. Today Python emits `Item` and `Item2`, which name flips if registration order changes;
TypeScript silently gives both topics one schema; .NET throws. Under rule 4 every port fails with
"component name `Item` claimed by `orders.Item` and `payments.Item`; declare a schema name". After
`OrderItem`/`PaymentItem` are declared, Go and Python emit identical keys.

**Ex 4: recursion.** `TreeNode { name: string; children: TreeNode[] }` hoists with a self-`$ref`.
The projection inlines it as
`{"properties":{"children":{"items":{},"type":"array"},"name":{"type":"string"}},"required":["name"],"type":"object"}`,
so the same type named `Node` hashes identically (`1b0f7e45…`).

### 4.4 The schema-content rule: why the derivation rule moves to 3a

Matching names do not give identical documents if the schema *content* differs. Today it does:
.NET's contract document comes from Swashbuckle (`nullable: true`, `format: double`,
`additionalProperties: false` everywhere, validation prose in `description`), while its descriptor
comes from `MeshSchemaGenerator` (the §2.1 mapping). The plan moves mesh §2.1 into contract-document
in 3b. It belongs in **3a**, because 3a is the first time the contract document's derivation is
tested across ports (`derivationCases`, §7). Rewritten for the OpenAPI 3.0 dialect the document
declares:

- nullable primitive, array or map T → T's schema plus `"nullable": true` (OpenAPI 3.0 has no type
  arrays; Go's `openapi.splitNullableType` already converts);
- nullable object or enum T → `{"nullable": true, "allOf": [<T or its $ref>]}`, so hoisted and inline
  forms dereference identically;
- records carry no `additionalProperties`; `required` per §2.1 (what the marshaler always emits);
- a handler that declares no response payload → `response: {}` (the descriptor rule; .NET's `Void`
  component goes; ruling Q5);
- enums and integer `format` are open (Q5).

**Cost:** .NET **M–L**. The benzene document has to come from a hoisting version of
`MeshSchemaGenerator` (already camelCase, already the §2.1 mapping) rather than Swashbuckle. The
OpenAPI and AsyncAPI exports MAY keep Swashbuckle, since they are not tested. Go M (part of the new
producer). TypeScript S: the canonical runner maps exact schemas, and real validator schemas are
legitimately richer. Python S (`json_schema` already follows §2.1; add the 3.0 nullable form).

---

## 5. The `{type, format}` request on `benzene:spec`

| | .NET | Go | TypeScript | Python |
|---|---|---|---|---|
| Topic | handler, `SpecRequest` | none | handler, `SpecRequest` | interception, request ignored, **native shape** |
| Unknown `type` | benzene document | — | benzene document | anything but `native` → contract document |
| HTTP | `GET /benzene/spec` via `httpMappings`; query copied onto the body by the request enrichers (`AspNetRequestEnricher`, API Gateway `QueryStringFirstWinsMapper`) | `SpecHandler` (the descriptor) | as .NET | path special-cased in `http/app.py` |

**Recommendation:**

- **Request:** `{ "type"?: string, "format"?: string }`. An absent body, an empty body, `null` and
  `{}` all mean `type: "benzene"`, `format: "json"`. Values are lowercase ASCII and compared
  case-insensitively.
- **Response:** `ok` with the contract document as the payload. Other values (`openapi`,
  `asyncapi`, `yaml`, Python's `native`) are port extensions (MAY), not pinned.
- **An unsupported `type` or `format` is answered with `not-implemented`**, not a silent default.
  The .NET aggregator asks every service for `type: asyncapi` (`MeshAggregator.cs`,
  `LambdaMeshServiceSource.TryFetchSpecAsync`). A Go or Python service would hand it a contract
  document labelled as something else.
- **HTTP:** the HTTP binding's default mapping is `GET /benzene/spec` → `benzene:spec`, with the
  `type` and `format` query parameters copied onto the request (this mapping's own rule, not a
  general query-binding rule). The body is the payload (`application/json` for the default), and the
  status follows the ordinary mapping (`not-implemented` → 501).
- **Conformance:** only the default output is tested: `specTopicCases` (no body, `{}`, and
  `{"type":"benzene","format":"json"}` → `ok` plus the derived document; `{"type":"conformance-x"}`
  → `not-implemented`), plus one `GET /benzene/spec` case for ports with an HTTP binding.
- D13's wording ("optional `{type, format}` request; the contract document by default") already
  matches. Python's topic has to start answering with the contract document: in its Phase 1
  catch-up if D13 is read strictly, in 3a at the latest.

**Cost:** .NET and TypeScript S (`not-implemented` for unknown values). Python S. Go M (the topic and
the selector, on top of the producer). **Consumers:** the .NET aggregator (it already tolerates a
failed AsyncAPI fetch), the Spec UI's default URL (D4's `/benzene/spec?type=benzene` keeps working),
the `benzene-descriptor --emit spec` tool.

---

## 6. Fields: keep, drop, move

### 6.1 The document after 3b

```json
{
  "schemaDialect": "openapi-3.0",
  "service": "orders", "serviceVersion": "1.4.2", "versionScheme": "semver",
  "description": "Order intake",
  "runtime": "go", "placement": { "cloud": "aws", "region": "eu-west-1" },
  "profile": { "name": "cloud-service", "missing": ["R6"] },
  "degraded": ["sends"],
  "messageEndpoint": "/benzene/invoke", "transports": ["http", "sqs"],
  "contractHash": "sha256:…", "registrationHash": "sha256:…",
  "requests": [ ], "events": [ ], "sends": [ ],
  "components": { "schemas": { } }
}
```

### 6.2 Field by field

| Field | Decision | Phase | Consumers affected |
|---|---|---|---|
| `openapi: "3.0.1"` | **Replace** with `schemaDialect: "openapi-3.0"` (REQUIRED). A consumer MUST reject a dialect it does not know. This is the discriminator the post-1.0 JSON Schema 2020-12 decision needs. The document stops claiming to be OpenAPI (review §4.9). | 3a | benzene-ui `ServiceSpec.openapi` (type only); Go `contractdoc.OpenAPI()`; `parseCases`. Python's `is_contract_document` already ignores it. |
| `info.title` | **Move** → `service` (REQUIRED) | 3a | benzene-ui `SpecPage`, `specSlice`, `selectors.ts` (title, version, description); Python `specdoc.spec_service`; .NET picks one of its two names (F10) |
| `info.version` | **Move** → `serviceVersion` (OPTIONAL) | 3a | as above |
| `info.description` | **Move** → `description` (OPTIONAL) | 3a | benzene-ui shows it |
| `tags` | **Drop** (already unpinned and never populated) | 3a | .NET serializer only |
| `requests[].reserved` and the reserved entries | **Drop.** A producer MUST NOT list `benzene:`-prefixed topics in `requests`, `events` or `sends`; their shapes are D13's table. A consumer MUST ignore any it meets. This removes contract-document §5.1's two-condition detection, §6.2's reserved-entry rule, the `includeReserved` generator option, and three `topicScopeCases`, one parse case and one hash case. | 3a | benzene-ui: the Spec page's utilities count and section (`selectUtilityOperations`, `SpecSummary`) go, or are fed from D13's list (Q3). The topic chips come from the aggregator's `reserved`, which it now computes from the prefix. .NET `AsyncApiCompositor` filters with `BenzeneTopic.IsReserved` instead of the spec flag. `IncludeReservedTopics` in four generators. The TypeScript Spec UI. |
| `example` | **Keep, OPTIONAL**, and SHOULD carry only an author-supplied example. Producers stop synthesizing (only .NET does). Outside P. | 3a | benzene-ui `SpecOperation` falls back to its own `exampleFromSchema`, which gains `$ref` resolution. .NET keeps `ExamplePayloadBuilder` for `benzene:test-payloads`. |
| `messageEndpoint` | **Keep**, OPTIONAL: the send-capability signal | — | benzene-ui `SpecSummary`; Spec UI try-it |
| `transports` | **Keep**, OPTIONAL; absorbs the descriptor's `binding` | 3a / 3b | collectors' fleet read model `binding` → `transports` (Provisional, D5); benzene-ui's `FleetView` type only |
| `components` | **Keep**, REQUIRED: codegen type names; benzene-ui's schema reference section | — | — |
| `service` | REQUIRED, the catalog key | 3a | all collectors |
| `serviceVersion` + `versionScheme` | Keep `serviceVersion`; **add** `versionScheme` (F7), REQUIRED when `serviceVersion` is present (service-versions §2) | 3a / 3b | collectors' ordering; .NET `Benzene.Descriptor` stops discarding `--version-scheme` |
| `instanceId` | **Drop** from the document. Heartbeats and traces carry it, and no collector reads it at registration (.NET, Go `meshd`, TypeScript and Python all key instances off heartbeats). It would also make a served document differ per replica. | 3b | none |
| `runtime`, `placement` | **Keep**, OPTIONAL; now outside the hash | 3b | collectors' fleet view |
| `profile` | **Keep**, OPTIONAL (documented in Phase 4) | 3b | Go adds it; probes |
| `degraded` | **Keep**, OPTIONAL. Values become member names (`requests`, `events`, `sends`) instead of feed names (`registry`, `outbound-registry`). | 3b | .NET `IsDeclared`, other collectors' equivalents |
| `binding` | **Drop** (→ `transports`) | 3b | collectors |
| `descriptorHash` | **Replace** with `contractHash` + `registrationHash` (REQUIRED, §3.4) | 3a / 3b | collectors, heartbeats, drift checks |
| `topics` / `produces` | → `requests` / `events` ∪ `sends` | 3b | collectors |

---

## 7. Migration and fixtures

### 7.1 3a: `spec-0.3.0`, "the document is one derivation, hashed one way"

**Spec:** contract-document §1–§6 rewritten (fields per §6, `sends`, the derivation rule moved from
mesh §2.1, the naming rule, the projections and the extended §6.4). The `benzene:spec` row of the
reserved-topic section gains the request and response detail. mesh.md is untouched apart from a
forward note, since the descriptor is still served until 3b.

| Fixture | 3a change |
|---|---|
| `contract-document-cases.json` | `documents` re-cut (`schemaDialect`, `service`, no reserved entries, one document with `sends`). `parseCases`: the reserved-detection case is replaced by "a `benzene:` entry is ignored"; a `sends` round-trip case is added. `topicScopeCases`: the three `includeReserved` cases go; "an include-list leaves `sends` untouched" is added. `schemaClosureCases` unchanged. **New** `derivationCases`: canonical handlers, event and sends → expected entries (dereferenced comparison), valid keys, resolving refs, exact `contractHash`/`registrationHash`. **New** `namingCases` (a collision fails; a declared name is honoured). **New** `specTopicCases` (§5). |
| `contract-hash-cases.json` | Every value recomputed. Cases: minimal; decorated (identity, hashes, `example`, endpoint and transports ignored); hoisting-independence (Ex 2); rename-independence; order-independence; annotation-independence; a cycle (Ex 4); `sends` moves `registrationHash` only (Ex 1); topic-scoped; include-list. |
| `conformance/README.md` | The canonical outbound becomes event `conformance:greeted {greeting}`, one-way send `conformance:log {message}` and send `conformance:lookup {key} → {value}`, with the canonical type names of §4.2. Claims: **contract-document producer** (`derivationCases`, `specTopicCases`; required by R5) is separate from **client generator** (parse, scope, closure, hash). |
| `mesh-descriptor-cases.json` | Unchanged; the descriptor is still on the wire. |

**Order:** (1) spec PR, fixtures, tag. (2) **benzene-ui**: spec readers (`service`/`serviceVersion`/
`description`, `schemaDialect`, no `reserved`, the Sends section, the example fallback), then rebuild
`mesh-spec-ui.html`, because .NET re-embeds it under a drift check. (3) .NET (the reference producer
and the derivation switch, the codegen clients, `SchemaHealthCheck`, the aggregator reading events ∪
sends, the compositor), TypeScript and Python in parallel. Go in parallel too, and longest: a new
producer, the `benzene:spec` topic and service-side JCS. (4) Re-vendor the UI pages into `mesh-ui/`
and the website demos. **Exit:** all four producers pass `derivationCases` with the same two hashes
for the canonical service.

### 7.2 3b: `spec-0.4.0`, "the document is the register body"

**Spec:** mesh §1 deleted; §2 reduced to "the register body is the contract document"; §2.2 and §2.3
deleted (moved in 3a); heartbeat per §3.4; the graph per §2.3; R6's first bullet ("the reserved
`benzene:mesh` topic") reworded to the self-description at `benzene:spec`, a one-line edit ahead of
Phase 4's re-cut.

| Fixture | 3b change |
|---|---|
| `mesh-descriptor-cases.json` | **Deleted.** Its derivation half has lived in `derivationCases` since 3a, and its hash-properties block is superseded by the exact `registrationHash`. |
| `contract-document-cases.json` | `derivationCases` gain `serviceInfo` (service, `serviceVersion` + `versionScheme`, placement), asserted on the document |
| `mesh-collector-cases.json` | Nine register steps rewritten as documents (`requests` with `{}` schemas, `events`, `sends`); `descriptorHash` → `registrationHash` on register and heartbeat; heartbeats carry `serviceVersion`. New cases: an `events` entry yields a provider edge; `benzene:` entries in a body are ignored. A body with only `service` is still accepted (§6 degradation). |
| `mesh-service-version-cases.json` | Nine register steps rewritten, on top of D26's Phase 1 amendment |
| `mesh-issue-cases.json`, `mesh-trace-cases.json` | Unchanged (no register steps; `missingFeeds: ["descriptor"]` is collector vocabulary) |
| benzene-ui `contracts/` | Drop the unused `mesh-descriptor-cases.json` |

**Order:** (1) spec PR, fixtures, tag. (2) In each port: the announcer sends the document, the
heartbeat sends `registrationHash` and `serviceVersion`, and the collector reads `requests`/`events`/
`sends`. The probes (.NET `CloudServiceProbe`, Go `cloudserviceprobe`, TypeScript
`CloudServiceProbe.ts`, Python `probe.py`) move from `benzene:mesh` to `benzene:spec`. benzene-ui is
not on this path (F8); the plan's "benzene-ui first (it reads `produces`)" can go. (3) Live interop:
a Go collector with a .NET service, and the reverse.

---

## 8. Cost per repo

| Repo | 3a | 3b |
|---|---|---|
| **benzene** (spec) | contract-document rewrite; two fixture files re-cut, three new groups; README canonical outbound + names | mesh §1–§2 collapse; heartbeat; R6 bullet; collector fixtures |
| **benzene-dotnet** | **M–L.** The derivation switch away from Swashbuckle; naming hook; events/sends split (`Publish<T>`); `schemaDialect`/`service`; no reserved, no synthesized examples; `ContractHash` projection; `SchemaHealthCheck`; codegen drops `IncludeReservedTopics`; aggregator and compositor; `benzene:spec` → `not-implemented`; AwsMesh example split | **S–M.** One builder for spec and register (`DescriptorEmitter` emits one artifact); `MeshAnnouncer`; collector; probe; `versionScheme` carried |
| **benzene-go** | **L.** A contract-document producer (hoisting from `reflect` names, the OpenAPI 3.0 dialect, the naming rule); the `benzene:spec` topic and selector; `SpecHandler` serves the document; `RegisterEvent`/`RegisterSend`; JCS in the root module; codegen hash projection | **M.** `meshd` ingest; announcer; `clienthealthcheck` → `contractHash`; `profile` field; probe |
| **benzene-typescript** | **S–M.** Stop mixing senders into `events`; collisions fail; naming hook; hash projection; `SchemaHealthCheck` → `contractHash`; codegen drops reserved options; Spec UI page | **S–M.** Collector; announcer; probe |
| **benzene-python** | **S–M.** `publish`/`send` split; `derive(events=, sends=)`; collision error; `benzene:spec` topic → contract document; hash projection; `specdoc` reads `sends` | **S.** Collector; announcer; probe |
| **benzene-ui** | **S.** Spec readers, Sends section, example fallback with `$ref` resolution, utilities panel (Q3); rebuild | none (F8), unless the fleet read model's `binding` → `transports` lands |
| **the-count** | none (F9) | none |

---

## 9. Risks and rulings

### 9.1 Rulings needed

| # | Question | Recommendation |
|---|---|---|
| Q1 | Name-independent hashing (dereference), which narrows D18's "normative component-naming rule" to validity, uniqueness, determinism and a default | **Yes.** It fixes F4–F6 without asking any port to rename or re-hoist. |
| Q2 | Keep `httpMappings` in both hashes? | **Keep.** HTTP is a Core binding and a route change reaches HTTP callers. Accept the false drift for transport-agnostic generated clients. |
| Q3 | Reserved topics: drop them entirely, or keep a names-only `reservedTopics: string[]` (unhashed) for benzene-ui's utilities panel? | **Drop.** The profile claim (Phase 4) says what a service provisions. Add the list only if the product owner wants the panel kept. |
| Q4 | Strip `title`/`description`/`example`/`x-*` from schemas before hashing? | **Yes.** They are documentation, and .NET puts validation prose there. |
| Q5 | Derivation rule details: a no-response handler (`{}` vs. `Void`); enums (`{}` as in §2.1 today, or `enum` with what the marshaler writes); integer `format: int64` (Go and .NET know it, Python and TypeScript cannot) | `{}`; describe what the marshaler writes and fall back to `{}`; integer `format` MAY, and outside P |
| Q6 | Should `sends` be public on `/benzene/spec`? (It is the service's dependency list; mesh §1 already let a deployment withhold the descriptor pending a security review.) | **Yes, one document.** `benzene:spec` runs the same pipeline and authentication as every topic (D21's posture). No addresses ever appear (§2.3). |
| Q7 | Do both hashes ride in the document? | **Yes, REQUIRED.** One shape for spec and register, and drift checks compare a served value. |
| Q8 | Go: take `gowebpki/jcs` into the root module, or hand-write a canonicaliser? | **Take the dependency.** A hand-written JCS is exactly the risk contract-document §6.3 warns about. |
| Q9 | Move the §2.1 derivation rule and `derivationCases` from 3b to 3a | **Yes** (§4.4) |
| Q10 | Heartbeat gains `serviceVersion` | **Yes** (§3.4) |

### 9.2 Risks

- **.NET's generator switch is the critical path.** Swashbuckle drives inheritance and polymorphism
  options (`SchemaGenerationOptions`, `allOf`/`oneOf` + discriminator), bring-your-own schemas
  (`SuppliedSchemaBuilder`) and validation-derived constraints (`OpenApiValidationSchemaBuilder`).
  A hoisting `MeshSchemaGenerator` must cover them or explicitly scope them out of the benzene
  document. *Mitigation:* the `derivationCases` canonical types use none of these, so .NET can
  land the switch for plain records first and port the options behind it.
- **Go's scope is a producer, not a re-cut** (F1). If Go slips, the plan's fallback applies (freeze
  Core; the self-description module stays Provisional). Go's `asyncapi`/`openapi` packages show the
  dialect conversion is already understood.
- **Register-body size.** The document carries `components`, `httpMappings` and optional examples,
  and goes over SQS, SNS, EventBridge and Lambda-async, which are all capped at 256 KB. Hoisting
  usually makes it *smaller* than today's descriptor, which repeats inline schemas per topic, and
  dropping synthesized examples helps. *Mitigation:* the collector topics already run over any
  envelope transport, so claim-check (Provisional) is the escape hatch. State the limit in mesh §4.
- **Every embedded hash changes once.** All generated clients have to be regenerated. This is
  accepted pre-1.0; no installed consumer has a generated client (F9).
- **Validator-sourced schemas (TypeScript)** carry keywords (`minLength`, `$schema`,
  `additionalProperties: false`) that reflection-derived schemas do not. That is fine across services,
  because the hash compares a service with itself, but the canonical runner must map exact schemas.
- **Interim double-shape period.** Between 3a and 3b a service serves the new document at
  `benzene:spec` and the old descriptor at `benzene:mesh`, so two derivations exist for one
  phase. They are already separate code in every port, so this is no regression.

### 9.3 Where this departs from the plan or review, with evidence

1. **Providers are events ∪ sends, not `sends` alone** (plan 3b). A published event's publisher is
   its provider; `order:placed` would otherwise have none (§2.3).
2. **The derivation rule moves in 3a, not 3b** (plan 3b). The contract document's cross-port
   derivation is first tested in 3a, and .NET's document and descriptor use different generators
   today (§4.4).
3. **benzene-ui is a 3a consumer, not a 3b one** (plan 3b, review §4.13). It reads aggregator
   artifacts, the contract document and the fleet query, never a register body (`src/data/meshApi.ts`,
   `contracts/generated.ts`).
4. **The sends-into-events mixing lives in TypeScript, Python and .NET applications, not the .NET
   builder** (review §2.1). `SpecBuilder.cs` never feeds sender definitions;
   `examples/AwsMesh/Shared/MeshServiceWiring.cs` and `SpecBuilder.ts` do.
5. **"The four contract generators"** (plan 3a): Go has none (F1). `docs/capabilities.md`'s Go "yes"
   for R5 should be re-read when Phase 3 closes.
6. **"A normative component-naming rule"** (D18): recommended as normative but not load-bearing (Q1).
7. **contractHash today covers `info`, entry order and component names** (contract-document §6.2:
   "`info` … hashed as they stand"). Together with .NET's #170 narrowing, this is why the drift check
   fails in practice (F4, F5). The review did not list it. It strengthens D18's case.

---

## 10. Port champion review (2026-10-11)

The design was reviewed, before ruling, by each port's DX champion — Go, TypeScript, Python, and
the .NET DX champion — against its own port's code (the plan's 0.2 asks for exactly this). All four
endorse the core design: two outbound lists, one algorithm over a dereferenced projection, names
outside the hash, the `{type, format}` request, reserved entries dropped. Three of them reproduced
parts of it independently (Python's prototype yields Ex 1's three hashes exactly; .NET's and Go's
scratch probes confirm the derivation claims). What they changed is the **cost** and a set of
**amendments**, below. Where this section and §0–§9 disagree, this section wins.

### 10.1 Revised cost

| Repo | §8 said | Now | Why it moved |
|---|---|---|---|
| benzene-dotnet | M–L, critical path | **M**, not critical | Swashbuckle does not need replacing: `SupportNonNullableReferenceTypes` + `UseAllOfToExtendReferenceSchemas` + one ~15-line schema filter produce §4.3's `GreetRequest` and §4.4's nullable ref exactly (probed), and bring-your-own schemas, validation constraints, polymorphism options, the collision error and `SchemaIdSelector` all stay. One generator then serves the benzene, OpenAPI and AsyncAPI formats, which also keeps their nullability in agreement. |
| benzene-go | L | **M** for the 3a exit; naming trails | Q1 takes names out of the hash, and Go's current all-inline schemas already give the canonical hashes. The 3a exit needs the existing walker, the OpenAPI-3.0 nullable rewrite (reuse `openapi.toOpenAPISchema`), `struct{}` → absent response, the projection, a canonicaliser, the topic and the registration split. Hoisting, naming and the name hook gate only `namingCases` and may trail as a recorded matrix gap; gengo's nullable/inline-object typing (S–M) was uncounted. |
| benzene-typescript | S–M | **M** | Zod output is draft 2020-12, not the OpenAPI-3.0 dialect (`anyOf` nullables, `additionalProperties:false`, recursive `$ref: "#"` that the port's own codegen rejects); class names do not survive bundling (esbuild renames a clash `Item2`, `--minify` renames it `e`); there is no outbound registry to split — `withProduces` feeds only the descriptor. |
| benzene-python | S–M | **M** | The hash needs RFC 8785 in the dependency-free `benzene-core`; `GET /benzene/spec` bypasses the pipeline; and real services serve `ContractDocument.from_spec`, not the `derive` path §2.4 changes. |

No port is L. The riskiest single items are TypeScript's Zod normalisation and Python's
merge of three producer paths into one, not .NET.

### 10.2 Amendments the four agree on

1. **Q4, wider strip list.** Strip the JSON Schema metadata vocabulary before hashing: `title`,
   `description`, `default`, `examples`, `example`, `deprecated`, `readOnly`, `writeOnly`,
   `$comment`, `$id`, `$schema`, and `x-*`. Pydantic writes `default` on every defaulted field and
   uses plural `examples`; Zod passes `.meta()` keys through. State that **validation constraints
   stay in the hash** — adding a validation rule is a contract change — and that other custom keys
   count too.
2. **Q5, `format`.** Producers SHOULD emit a `format` they know (`int64`, `uuid`): .NET's generated
   clients need it, or `long` becomes `int` — silent 64-bit truncation — and `Guid` becomes
   `string`. §3.2 step 4 strips `format` on every schema, so it never reaches the hash. (Go's and
   TypeScript's "never emit it" stays conformant.)
3. **Q5, no response.** An **absent** `requests[].response` means "no payload", exactly as in
   `sends`; `{}` keeps meaning "any payload". Emitting `{}` for a no-response handler makes .NET
   codegen produce `IBenzeneResult<>`, which does not compile. In Go, an unnamed `struct{}` maps to
   absent; a named empty struct stays an object.
4. **Q5, enums.** A port that knows the values emits a typed `enum` (Zod; a `str`/`int`-mixin
   Python `Enum`); one that cannot list them falls back to the **base type** (Go's
   `type Level string` → `{"type":"string"}`), and only an unserializable type falls back to `{}`.
5. **Nullable references.** OAS 3.0.3 ignores `nullable` without a `type`, so the derivation rule
   emits `{"type":"object","nullable":true,"allOf":[…]}`. A reference cut at a cycle normalises to
   `{}` in P whether or not it was marked nullable. `derivationCases` pins both shapes.
6. **§3.2 step 5.** Upper-case `httpMappings[].method` in P, not only sort by it — one .NET document
   already holds both `"POST"` and `"get"`. TypeScript also attaches `httpMappings` by topic,
   ignoring version; fix it now that they are in P.
7. **Collisions fail before the first message, not lazily.** .NET, TypeScript and Python build the
   spec on the first `benzene:spec` call. The rule is D23's: verified before any message is handled
   — at build time where the port has one (.NET's `benzene-descriptor` / CodeGen.Build, with an
   opt-in start-up check to spare Lambda cold starts), at wire-up otherwise (Go: `Derive(...)
   (Document, error)`). The error names the types, the topics that use them, and the fix.
8. **The hash moves into each runtime package** (Q7). .NET: `ContractHash`/`JsonCanonicalizer` into
   `Benzene.Schema.OpenApi`, type-forwarded from `Benzene.CodeGen.Core` (and
   `Benzene.HealthChecks.Schema` stops shipping codegen into deployed services); TypeScript: a new
   `@benzenejs/codegen-core`; Python: `benzene-core`; Go: a root package. Every private hash goes:
   TypeScript's aggregator HMAC (`MeshSnapshotBuilder.ts`, a fourth hash), Python's poller
   `_spec_hash`, and .NET's `SchemaHealthCheck`, which rebuilds the hash per call from a plain
   `SchemaBuilder` — another cause of F4. Health checks and pollers read the **served** hash.
9. **Q6 has a precondition two ports miss.** `benzene:spec` behind the same pipeline and
   authentication only holds if `GET /benzene/spec` goes through the pipeline. Python's
   `_handle_standard` serves it before the pipeline runs; Go's `mesh.SpecHandler` is a plain
   `http.Handler`. Both must route it through the pipeline before `sends` appears in the document.
10. **benzene-ui reads both shapes during the transition.** .NET's `mesh-ui-drift-check` clones
    benzene-ui's default branch, so a UI that only reads the new document turns .NET red until it
    re-vendors.

### 10.3 Per-port API shapes (idiom, recorded for each port's 3a)

| | Event / send declaration | Declared-name hook |
|---|---|---|
| .NET | Already exists: events from `AddResponseEventDeclarations` (`ResponseEventCatalog`), sends from `MeshOutboundRegistry` via the existing `IMessageDefinitionFinder<IMessageSenderDefinition>` — no `Publish<T>`, no new project reference. Rename `WithConsumes` → `WithSends` (keep an `[Obsolete]` alias). Generated clients register their own sends from `RequiredTopics`. | Swashbuckle `SchemaIdSelector`, surfaced as `[SchemaName("…")]` |
| Go | Delete `RegisterOutbound` before 1.0 so every call site chooses (today's `RegisterOutbound[T, any]` would silently become `response: {}`). `RegisterEvent[T](r, topic)`, `RegisterSend[TReq](r, topic, opts...)` with `WithResponse[TRes]()`; `Must*` twins; one registry map, so "not in both" is the existing duplicate check. A root `contract` package holds document, derivation, middleware and hashing (avoids a mesh import cycle); alias `mesh.OutboundRegistry` for one release. | `BenzeneSchemaName() string` (value receiver, constant); undeclared generic instances inline |
| TypeScript | Port `MeshOutboundRegistry`, registered in the container so `SpecBuilder` and the announcer both read it; discriminated `{ kind: 'event' \| 'send' }`; `withProduces` a deprecated alias. | `MapTypeJsonSchemaSource.register(T, schema, { name })` and Zod `.meta({ id })`, then `ctor.name`; cookbooks set `keepNames: true` |
| Python | `register_event(topic, message_type=None, *, version="")` and `register_send(topic, request_type=None, response_type=None, *, version="")` — not `publish`/`send`, which read as actions next to `send_message`; no kind-less `register` (a `TypeError` names both); `OutboundRegistry` moves to `benzene.core`; the bare-string-is-a-send shortcut goes. | `@schema_name("…")` class decorator (pydantic `title` is stripped by Q4 and never renames a `$defs` key) |

Cross-port naming: where a port has a `register` verb, `register_event` / `register_send` (Go and
Python agree); the spec names the two concepts **event** and **send** declarations and leaves the
API to each port.

### 10.4 Per-port additions to 3a

- **.NET:** the upgrade path — the documented compatibility gate (`contract-testing.md`) would fail
  every user's CI: `SchemaCompatibilityComparer` must ignore `benzene:` topics, a baseline with no
  `schemaDialect` produces one "re-baseline" error rather than dozens of false breaking changes,
  and the reader accepts legacy documents with a warning. `IApplicationInfo` becomes the single
  source for `service`/`serviceVersion` (`CloudService/Extensions.cs` already sets the name). The
  dereferencer is `MeshAggregator.InlineSchema`, with its cycle cut changed to `{}`. The conformance
  type `GreetReply` is renamed or used for `namingCases`.
- **Go:** `clienthealthcheck` reads `contractHash` from `benzene:spec` in 3a (Go's actual F4 fix);
  `asyncapi.Generate` reads `events` from the document and `WithSentEvent` goes (it lives in
  `asyncapi`, not `openapi`); `cloudservice.New` returns an error; the probe waits for 3b.
- **TypeScript:** Zod with `{ io: 'input', target: 'openapi-3.0' }`, `$defs` hoisted into
  `components.schemas` and `#` rewritten — after which the runner passes `derivationCases` on real
  Zod output instead of hand-mapped schemas; Joi and Yup converters keep nullability (both drop it
  today); `SchemaHealthCheck` hashes the full document; `Benzene.Spec.Ui`'s inline page stops
  reading `info.title`/`reserved`.
- **Python:** derive one document at the composition root (`AppDefinition` holds both registry and
  router) and serve it on both surfaces; retire `ServiceSpec`, `from_spec` and `?type=native`
  pre-1.0; `specdoc` cuts cycles with `{}`; codegen rejects an unknown `schemaDialect` and drops
  `--include-reserved`; the poller reads `registrationHash`.

### 10.5 Rulings, restated after review

**Ruled 2026-10-11 by the maintainer: every recommendation below is accepted** — Q1–Q7, Q9 and Q10
as recommended, Q8's two dependency exceptions (Python's `rfc8785` in `benzene-core`; Go's
hand-written, fuzz-checked `internal/jcs`), and Q11 deferred as a recorded Python gap.

| # | Ruling | Recommendation now |
|---|---|---|
| Q1 | Name-independent (dereferenced) hashing | **Yes** — unanimous; TypeScript's bundler finding makes it essential |
| Q2 | `httpMappings` in both hashes | **Yes**, with method upper-casing and TS's version fix |
| Q3 | Drop reserved topics from the document | **Yes** — unanimous; .NET's upgrade-path fixes land with it |
| Q4 | Strip metadata before hashing | **Yes, the wider list** of 10.2.1; validation constraints stay in |
| Q5 | Derivation details | **Amended:** absent response = no payload; `format` SHOULD be emitted and is stripped; enums typed where known, else base type |
| Q6 | `sends` public on `/benzene/spec` | **Yes**, once Go and Python route `GET /benzene/spec` through the pipeline |
| Q7 | Both hashes in the document | **Yes**; the hash moves into each runtime package |
| Q8 | How each port gets RFC 8785 | **Reframed:** the spec requires RFC 8785 output, proven by `contract-hash-cases.json`; each port chooses how. Two need your approval because they touch dependency rules: **Python** takes `rfc8785` (≈280 lines, no dependencies) into `benzene-core`; **Go** hand-writes `internal/jcs` (~100 lines) with a differential fuzz test against `gowebpki/jcs` inside `codegen`, which already depends on it — `gowebpki/jcs` itself would pull an unpruned test-dependency graph into the zero-dependency root |
| Q9 | Derivation rule in 3a | **Yes** — unanimous |
| Q10 | Heartbeat gains `serviceVersion` | **Yes** — unanimous |
| Q11 | *new:* Python's pydantic models describe themselves as `{}` today (`json_schema` returns `{}` for a `BaseModel`) | **Defer** past 3a as a recorded Python gap: fixing it is M on its own (a schema-source hook in `benzene-pydantic`, validation vs serialization mode, rejecting pydantic's silent `orders__Item` renaming) and is orthogonal to the merge |
