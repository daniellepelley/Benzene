# Payload Testing UI Plan

## Context

AWS ships a Lambda Test Tool that lets a developer fire test payloads into a locally running
Lambda; Swagger UI lets a developer construct and send test requests into an HTTP API. Benzene
has no equivalent for its own native unit of work — the **topic**. A developer running a Benzene
service today can browse its topics in the Spec UI, but cannot construct a payload for a topic,
cannot get a ready-made demo payload, and cannot send one into the running service without
hand-crafting a transport event.

This plan adds that capability. It is deliberately **not** a Swagger replacement: for
HTTP-mapped endpoints Swagger/OpenAPI tooling is already best-in-class, and Benzene already
emits an OpenAPI spec for those. The gap is topic-centric and transport-agnostic: SQS/SNS/
EventBridge/Kafka/EventHub handlers have no "try it" story at all.

The plan is an iteration, not a green-field build. Three partial implementations already exist
in the repo and none is release-ready:

1. **Demo payload generation** — `src/Benzene.CodeGen.Core/PayloadBuilder.cs` walks an
   `OpenApiSchema` and emits a deterministic sample payload. Works, tested, but codegen-only and
   with real quality gaps (see below).
2. **Send-a-payload-over-HTTP** — `examples/Aws/Benzene.Examples.Aws/Extensions.cs` contains a
   prototype `UseHttpToBenzeneMessage` (POST `admin/benzene-message` → BenzeneMessage pipeline),
   ApiGateway-only, hardcoded path, inline `JsonConvert`, plus a half-finished, commented-out
   `AdminBenzeneMessageMiddleware`. The concept is proven; the implementation is example-grade.
3. **Lambda Test Tool file generation** — `src/Benzene.CodeGen.MockLambdaTool/` generates
   per-topic, per-transport saved-request JSON files, but is not wired into the CodeGen CLI and
   its namespace (`Benzene.CodeGen.LambdaTestTool`) doesn't match its package name.

## Verified facts this plan relies on

- **`PayloadBuilder` behaviour** (`src/Benzene.CodeGen.Core/PayloadBuilder.cs`): deterministic
  values (`string` → `"value"`, `integer` → `42`, `number` → `42.42`, `boolean` → `true`,
  `date-time` → fixed timestamp, `uuid` → fixed GUID); recursion guard for self-referencing
  schemas emits `new object()` (serializes as `{}`) / empty array; known-value overrides keyed
  by bare camel-cased property name only; forces `.Camelcase()` on every key. It **ignores**
  schema `enum`, `example`, `default`, `minimum`/`maximum`, `minLength`/`maxLength`, `pattern`,
  and every `format` other than `date-time`/`uuid` — so a generated payload can fail the very
  validation rules the same spec advertises (FluentValidation/DataAnnotations constraints are
  projected into the spec schemas, and `spec-ui.html` renders them as constraint chips).
  Golden-file tests exist at `test/Benzene.Core.Test/Autogen/CodeGen/Core/PayloadBuilderTest.cs`.
- **Envelope builders exist and are shipped**: `ExampleBuilder`/`HttpExampleBuilder`
  (`Benzene.CodeGen.Core`) wrap a generated payload in a transport envelope via injected
  delegates; `test/Benzene.Core.Test/Autogen/CodeGen/LambdaTestTool/LambdaTestToolBuilderTest.cs`
  shows them combined with `Benzene.Testing`'s `MessageBuilder`/`HttpBuilder`
  (`.AsSns()`, `.AsSqs()`, `.AsApiGatewayRequest()`).
- **The BenzeneMessage transport is the dispatch primitive**: envelope
  `BenzeneMessageRequest { Topic, Headers, Body }` → `BenzeneMessageResponse { StatusCode,
  Headers, Body }` (`src/Benzene.Core.Messages/BenzeneMessage/`), executed by
  `BenzeneMessageApplication` (`src/Benzene.Core.MessageHandlers/BenzeneMessage/`) through a
  pipeline tagged with the `"benzene"` transport name. Production adapters exist for direct AWS
  Lambda invoke (`BenzeneMessageLambdaHandler`, mounted by `UseBenzeneMessage` with two
  overloads — inline `Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>>` and shared
  pre-built builder) and Azure Event Hub. **No production HTTP adapter exists** — only the
  example prototype above.
- **The benzene spec** (`src/Benzene.Schema.OpenApi/EventService/EventServiceDocument.cs`) is
  `{ Info, Tags, Requests: RequestResponse[], Events: Event[], Components }`, where
  `RequestResponse = { Topic, Version, HttpMappings, Request, Response }`. It carries **no
  example payloads** today. It is served by the `spec` topic (`UseSpec`, see `docs/spec.md`)
  and rendered by `Benzene.Spec.Ui`.
- **`Benzene.Spec.Ui`** is a single self-contained 973-line `spec-ui.html` (inline CSS + vanilla
  JS, zero external requests, embedded as a resource) served by the transport-agnostic
  `SpecUiMiddleware<TContext> where TContext : IHttpContext`, which short-circuits GET/HEAD on
  its path by writing directly through `IBenzeneResponseAdapter<TContext>` (same shape as
  `CorsMiddleware`). It works on API Gateway, Azure Functions, ASP.NET Core, and self-host.
- **Dependency layout permits the placements below without new references**: `Benzene.Http`
  already references `Benzene.Core.MessageHandlers` and `Benzene.Core.Messages` (everything an
  HTTP BenzeneMessage adapter needs); `Benzene.Schema.OpenApi` references `Benzene.Http`;
  `Benzene.CodeGen.Core` references `Benzene.Schema.OpenApi`, so example generation can move
  *down* into `Benzene.Schema.OpenApi` and remain visible to codegen.
- `IHttpStatusCodeMapper` (`Benzene.Http`) is the existing mechanism for mapping a Benzene
  result status to an HTTP status code (the example prototype already uses it).
- The CodeGen CLI (`Benzene.CodeGen.Cli.Core/Commands/`) has Build/Confluence/HealthCheck/Spec
  commands; nothing invokes `LambdaTestFilesBuilder`.
- All `src/` projects are packable (`src/Directory.Build.props` sets `IsPackable=true`), so
  `Benzene.CodeGen.MockLambdaTool` ships today under its mismatched name.

## Goals / non-goals

**Goals**
1. Release-ready demo payload generation, driven by the same schema + validation metadata the
   spec already carries, available at runtime (not just codegen time).
2. A first-class, transport-agnostic way to send a topic + payload into a running Benzene
   service over HTTP — the equivalent of the Lambda direct-invoke BenzeneMessage path.
3. A UI to construct, edit, and send payloads per topic, with pre-built demo payloads —
   Swagger-"Try it out" parity, organised by topic.
4. Productize the Lambda Test Tool file generation that already exists.

**Non-goals**
- Re-doing Swagger for HTTP endpoints. HTTP-mapped topics remain documented/testable via the
  OpenAPI spec and Swagger UI; the UI here dispatches by **topic** through the `"benzene"`
  transport, regardless of whether an HTTP mapping also exists.
- A standalone desktop tool or IDE plugin.
- Mesh-level "send a message to any service in the mesh" (future work, noted at the end).

## Design decisions (final)

- **D1 — One example generator, homed with the spec.** The payload generator moves from
  `Benzene.CodeGen.Core` to `Benzene.Schema.OpenApi` (new `Benzene.Schema.OpenApi.Examples`
  namespace: `IExamplePayloadBuilder` / `ExamplePayloadBuilder`, plus `ISchemaGetter` /
  `SchemaGetter` which it needs). `Benzene.CodeGen.Core` keeps thin delegating types (or plain
  re-namespaced usage) so codegen, markdown, and Lambda-test-tool builders all consume the same
  generator. Breaking namespace move — acceptable under the current pre-1.0 clean-break posture.
- **D2 — Generator hardening (the "release ready" bar).** In priority order:
  1. Honour schema `example`, then `default`, then `enum` (first value) before falling back to
     type-based values.
  2. Cover the formats the schema builder and validation projection actually emit: `uuid`,
     `date-time`, `date`, `email`, `uri` (verify the emitted set against `SchemaBuilder` and the
     validation → schema projection while implementing; extend only for formats that occur).
  3. Respect derivable constraints so generated payloads pass the service's own validation:
     clamp numerics into `minimum`/`maximum`, size strings within `minLength`/`maxLength`.
     `pattern` is out of scope (no regex reverse-generation); when only a `pattern` exists the
     value falls back and the UI lets the user edit.
  4. Known values keyed by property **path** (`order.customer.email`) with bare-name fallback,
     preserving today's constructor shape as the fallback behaviour.
  5. Explicit recursion policy: self-referencing object → `{}`, self-referencing array → `[]`,
     bounded by a max depth (default 8) so mutually-recursive schemas terminate too.
  6. Determinism stays: fixed values, no randomness — golden files and spec caching depend on it.
  7. Keys keep today's camelCase output (that is what the wire format serializes); the
     `.Camelcase()` transform stays in the generator for now and follows the schema if schema
     casing is ever fixed at source.
  Existing golden-file tests move/extend with new cases: enum, example/default precedence,
  min/max clamping, email/uri formats, mutual recursion, path-keyed known values.
- **D3 — Examples ship inside the benzene spec.** `RequestResponse` gains an optional
  `example` (request payload example, serialized as a real JSON object, not a string), and
  `Event` gains the same for its message payload. Generated at spec-build time by the D1
  generator from `Components.Schemas`. Additive change to the spec format; the Spec UI renders
  it with a copy button. This makes demo payloads a **service capability** consumable by every
  client — Spec UI, codegen, the mesh, curl users — instead of a codegen-only artifact, and it
  keeps generation in C# where the validation metadata lives (no duplicate generator in JS).
- **D4 — BenzeneMessage-over-HTTP becomes a first-class adapter in `Benzene.Http`.** New
  `Benzene.Http.BenzeneMessage` namespace:
  - `BenzeneMessageHttpMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext`
    — on POST to its path (default **`/benzene-message`**): read the body via the request
    adapter, deserialize a `BenzeneMessageRequest`, run it through a supplied built
    `IMiddlewarePipeline<BenzeneMessageContext>` via `BenzeneMessageApplication`, then write the
    response and short-circuit; anything else falls through to `next`. Same short-circuit shape
    as `SpecUiMiddleware`/`CorsMiddleware`, so it works on every HTTP transport.
  - `UseBenzeneMessage(path = "/benzene-message", ...)` extension with the **same two overload
    shapes as the Lambda adapter** (inline `Action<IMiddlewarePipelineBuilder<BenzeneMessageContext>>`,
    and pre-built shared builder), same name for discoverability.
  - **Response contract:** HTTP status from `IHttpStatusCodeMapper.Map(response.StatusCode)`,
    `Content-Type: application/json`, body = the full response envelope
    `{ "statusCode": ..., "headers": ..., "body": ... }`. Scripts get a real HTTP failure code;
    the UI gets the transport-level result in one place. (The example prototype returned only
    the inner body; the envelope is strictly more useful and matches what direct Lambda invoke
    returns.)
  - The example app's ad-hoc `UseHttpToBenzeneMessage` and dead `AdminBenzeneMessageMiddleware`
    are deleted and replaced with the real adapter.
- **D5 — Security posture: opt-in, and advertised honestly.** The endpoint exposes **every
  registered topic** over HTTP, including topics with no HTTP mapping. It is never registered
  implicitly. Options object `BenzeneMessageHttpOptions { Path, Func<string, bool>? TopicFilter }`
  allows an allowlist; anything richer (auth) composes as ordinary middleware placed before it.
  Documentation states plainly: intended for local development and protected/admin environments;
  do not expose unauthenticated in production.
- **D6 — Endpoint discovery via the spec.** `UseBenzeneMessage` registers a singleton
  `IBenzeneMessageHttpEndpointInfo { Path }`; the benzene spec builder, when that service is
  resolvable, writes an optional top-level `messageEndpoint` field into the benzene-format spec.
  Consumers feature-detect: no field → no send capability.
- **D7 — The UI is an extension of `Benzene.Spec.Ui`, not a new package.** `spec-ui.html` gains
  a "Try it" panel on each topic card (requests *and* events):
  - JSON payload editor pre-filled from the spec `example` (D3), with a "reset to example"
    action; a simple key/value headers editor.
  - A Send button, shown only when the loaded spec advertises `messageEndpoint` (D6); it POSTs
    the `{topic, headers, body}` envelope and renders the response: status chip (reusing the
    existing chip styling), response headers, pretty-printed body, and round-trip duration.
  - Stays a single self-contained file: vanilla JS, inline CSS, `fetch` to the same origin the
    spec came from, no external dependencies.
  Rationale for not creating `Benzene.Test.Ui`: Swagger UI's precedent is docs + try-it in one
  page; the capability gate is server-side (the endpoint either exists or doesn't, and the page
  degrades to today's read-only viewer); and two ~1000-line self-contained HTML apps rendering
  the same spec would drift. If the panel ever needs a genuinely different lifecycle, splitting
  later is cheap because the send path is spec-driven.
- **D8 — Lambda Test Tool generation is productized in place.** The project is renamed to match
  its namespace (`Benzene.CodeGen.LambdaTestTool`), gets a `CLAUDE.md`, and a CodeGen CLI
  command that takes a spec (URL or file) plus output directory and writes the saved-request
  files using the D1 generator + the existing envelope builders. Per-transport envelope preview
  inside the UI is explicitly deferred (see Open questions).

## ⚠️ FLAGS — approved by approving this plan

- **Breaking:** `PayloadBuilder`/`IPayloadBuilder`/`SchemaGetter`/`ISchemaGetter` move namespace
  (`Benzene.CodeGen.Core` → `Benzene.Schema.OpenApi.Examples`). Pre-1.0 clean break, CHANGELOG
  entry required. No other public API changes to existing types; `RequestResponse`/`Event` gain
  optional members (additive).
- **Solution file change:** renaming `Benzene.CodeGen.MockLambdaTool` →
  `Benzene.CodeGen.LambdaTestTool` touches `Benzene.sln`. This is the explicit approval the root
  guide requires; if the rename is contentious it can be dropped from Phase 5 without affecting
  Phases 1–4.
- **No new NuGet dependencies. No new projects.** All work lands in existing projects
  (`Benzene.Schema.OpenApi`, `Benzene.Http`, `Benzene.Spec.Ui`, `Benzene.CodeGen.*`, examples,
  tests, docs).

## Phases

Phases run in order; each compiles, passes the full suite, and merges independently.

### Phase 1 — Example generator: relocate and harden (D1, D2)
- Move generator + schema getter into `Benzene.Schema.OpenApi.Examples`; update
  `Benzene.CodeGen.Core` consumers (`ExampleBuilder`, `HttpExampleBuilder`), markdown builder,
  and Lambda-test-tool builder.
- Implement D2 items 1–7 with golden-file tests (existing tests move; new cases added).
- Package `CLAUDE.md` updates for both packages.

### Phase 2 — Examples in the spec, rendered by Spec UI (D3)
- `RequestResponse.Example` / `Event.Example` + serialization; populate during benzene spec
  build; extend spec-format tests.
- `spec-ui.html`: render the example (collapsed by default) with a copy button on each topic
  card; update `docs/spec.md` + `docs/spec-ui.md`.

### Phase 3 — `UseBenzeneMessage` over HTTP (D4, D5, D6)
- Middleware + extensions + options in `Benzene.Http`; endpoint-info registration; spec
  `messageEndpoint` field.
- Tests mirror `SpecUiMiddlewareTest` (Moq'd `IHttpRequestAdapter`/`IBenzeneResponseAdapter`:
  POST-on-path dispatches and short-circuits; other methods/paths fall through; topic filter;
  malformed body → mapped bad-request envelope) plus a `BenzeneTestHost` integration test
  driving a real pipeline end-to-end.
- Replace the example app's prototype; document the security posture (new
  `docs/payload-testing.md`, linked from `docs/index.md`).

### Phase 4 — Try-it panel in Spec UI (D7)
- Payload editor, headers editor, send, response rendering, feature detection; keep the page
  self-contained and theme-aware.
- `SpecUiPage` tests stay as-is (no API change); add viewer behaviour notes to
  `docs/spec-ui.md`; extend `src/Benzene.Spec.Ui/CLAUDE.md`.

### Phase 5 — Lambda Test Tool productization (D8, stretch)
- Project rename, CLI command (`lambda-test-tool` verb: spec in, saved-request JSON files out),
  wiring `LambdaTestFilesBuilder` to the shared generator, tests for the command, README/docs.

## Open questions (defaults chosen; challenge in review)

1. **Default path** — `/benzene-message` (explicit, unlikely to collide) vs `/message`. Default:
   `/benzene-message`.
2. **Spec field name** — `messageEndpoint` vs `benzeneMessageEndpoint`. Default:
   `messageEndpoint` (the spec is already benzene-format-scoped).
3. **Events in the try-it panel** — sending to an event topic dispatches to the local handler,
   which is exactly what a developer testing a consumer wants, but it may surprise people
   expecting a broadcast. Default: allow it, label the button "Dispatch" on event cards.
4. **Per-transport envelope preview in the UI** (view payload as SQS/SNS/EventBridge event for
   copy-paste into AWS tooling): deferred. Options when picked up — embed per-transport
   `examples` variants in the spec (server knows its registered transports) vs JS templates in
   the viewer. Leaning spec-side, same reasoning as D3.

## Future work (out of scope)

- **Mesh integration**: the Mesh host (`deploy/Mesh`) already aggregates each service's benzene
  spec; once services advertise `messageEndpoint` (D6), the Mesh UI could offer cross-service
  payload testing from one dashboard. Requires the same security thinking as D5, multiplied.
- **Client SDK reuse**: `Benzene.Client.Http` could gain a client that targets the
  `UseBenzeneMessage` endpoint, giving service-to-service calls a topic-addressed HTTP path
  without per-endpoint HTTP mappings.
