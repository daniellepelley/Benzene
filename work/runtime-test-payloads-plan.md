# Runtime test-payloads endpoint — design plan

## Goal (from the maintainer)
> The lambda test-tool payloads are very AWS-specific but quite useful. It would be nice to deploy
> them when a lambda gets deployed — optionally — so you end up with a lambda on AWS where all the
> test payloads are available, and then you can test out the functionality easily. Split by
> transport: if the lambda does API Gateway and SNS, then each topic gets a payload dressed as an
> SNS message and as an API Gateway request. A mixture of introspecting the service to see what it
> does and then building the templates.

Turn the **build-time** `Benzene.CodeGen.LambdaTestTool` (CLI generates `{topic}-{transport}.json`
files) into an **optional runtime endpoint** on the deployed service that self-serves the same
payloads, dressed for only the transports that service is actually wired to.

## What already exists (reusable)
- `LambdaTestFilesBuilder` iterates `EventServiceDocument.Requests` × `IExampleBuilder[]` (transports)
  and emits `{topic}-{transport}.json`. Transport-dressing already implemented:
  `benzene-message`, `sns`, `sqs`, `api-gateway` (the last only for HTTP-mapped topics).
- `EventServiceDocument` is built at runtime already (that is exactly what `UseSpec` serves).
- `EventServiceDocument.Transports` (`string[]`) already lists every wired receive transport
  (from `ITransportsInfo`) — this is the "introspect what the service does" input.
- Per-topic HTTP reachability is already on `RequestResponse.HttpMappings`.
- The deterministic example generator now lives in `Benzene.Schema.OpenApi.Examples`
  (runtime-safe — it was moved out of `Benzene.CodeGen.Core` precisely so it can run during spec
  builds), so payload bodies can be generated at runtime with no codegen dependency.

## Proposed shape (first cut)
- New reserved utility topic `test-payloads`, served by `UseTestPayloads()` — mirrors `UseSpec()`.
  **Opt-in by construction**: nothing is exposed unless the developer adds `.UseTestPayloads()`, so
  there is no new always-on surface. (It exposes no more than `/benzene/spec` already does — the
  contract shape and example payloads are already public there — but staying opt-in keeps test
  tooling off prod unless deliberately switched on.)
- Response = a manifest: for each non-reserved topic, the transports that topic supports
  (intersection of `Transports` + `HttpMappings` for `api-gateway`), each with a generated example
  payload dressed for that transport.
- Selectable: `test-payloads` (manifest) and `test-payloads/{topic}/{transport}` (one payload).

## Open decisions (need maintainer input)
1. **Transport-dressing dependency.** The SNS/SQS/API-Gateway envelope builders
   (`MessageBuilder.AsSns()/AsSqs()`, `HttpBuilder.AsApiGatewayRequest()`) currently live in
   `Benzene.CodeGen.LambdaTestTool`, which depends on AWS **test-helper** packages. Running them at
   runtime would pull those into the service's runtime graph. Options:
   - (a) Extract the envelope-dressing into a small runtime-safe package both the CLI and the
     endpoint share (cleanest, more work).
   - (b) Endpoint serves only the raw `benzene-message` payload at runtime; SNS/SQS/API-GW dressing
     stays CLI-only (smallest, loses the "dressed per transport" runtime value).
   - (c) Endpoint takes an injectable `IExampleBuilder[]` so the AWS dressing is opt-in via a thin
     `Benzene.*.TestPayloads.Aws` package (keeps core runtime clean, AWS stays AWS-only).
2. **Scope of the first cut.** JSON endpoint only, or also the HTML pane to pick a topic+transport
   and *fire it* at the running service (bigger; pairs with the Spec UI)?
3. **Prod safety.** Opt-in registration only, or also require an explicit
   `AllowInProduction`/env-flag so a stray `.UseTestPayloads()` can't ship enabled by accident?

## Recommendation
Start with **1(c) + 2(JSON only) + 3(opt-in registration, env-flag optional)**: a runtime-clean
`Benzene.*.TestPayloads` core serving the manifest + per-topic/transport payloads, with AWS
dressing in a separate opt-in package, and the "fire it" HTML UI as a fast follow once the JSON
endpoint exists. This delivers the introspect-and-dress value with no AWS runtime coupling and no
new always-on surface.
