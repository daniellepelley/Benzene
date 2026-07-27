# Port Quality Standards — Definition of Done for Language Ports

**Status: DRAFT v0.1.** Cross-language. Applies to every Benzene language port — `benzene-python`,
`benzene-go`, `benzene-typescript`, and every future port — and to every unit of work within one.

The key words MUST, SHOULD, and MAY are used per [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119).

## Purpose

[porting-guide.md](porting-guide.md) says *what* to port and the [conformance fixtures](conformance/README.md)
prove the *wire* is correct. This document defines the **quality bar and the process** around a port
contribution: when a unit of port work is *done*, and the guard-rails that keep every port at a high,
proven, easy-to-adopt standard.

A **unit of port work** is a shippable increment: a transport binding, a cloud host, a cross-cutting
feature, or a package. A unit of work is **DONE** only when every *applicable* gate in §2 is green
**and** the language's DX champion has signed off (§1). "Applicable" matters — a pure-core feature
has no cloud example (G3/G4 N/A); a cloud host triggers all gates.

The whole point of these guards is to drive **high-quality, highly-tested code that is proven to work
end to end and is shaped by the best possible developer experience** — so the port is easy to adopt,
understand, and trust.

## 1. The workflow — the DX champion is in the loop, start and end

Every language port has a **DX champion** agent/role (e.g. `python-dx-champion`,
`typescript-dx-champion`) whose mandate is the balance between fidelity to the spec and naturalness
to that language's developers. The champion is engaged at **both ends** of every unit of work:

1. **Plan — champion engaged at the start.** Before implementation begins, the DX champion reviews
   (or co-authors) the plan: package boundaries and names, the public API shape, the language idioms,
   which transports the example will exercise, and the adoption level. Implementation MUST NOT start
   until the plan has had champion input.
2. **Build.** Implement to the gates in §2.
3. **Self-verify.** Conformance green, all tests pass, every example builds, CI green — locally,
   before review.
4. **Review — champion engaged at the end.** The DX champion reviews the finished work against the
   "feels-like-`<language>`" bar and every gate in §2, and produces a ranked list of findings.
5. **Action feedback in a cycle.** Every finding is actioned (or explicitly waived with a recorded
   reason), then re-reviewed. Repeat until the champion signs off. **Sign-off is a required, recorded
   step**, not a formality.
6. **Docs + done.** Documentation is updated (G6) and the unit of work is complete.

A silent skip of either champion touch-point is a process failure, regardless of how good the code is.

## 2. The quality gates (the Definition of Done)

| Gate | Requirement |
|---|---|
| **G0 — Baseline** | Builds clean; the language's linter/type-checker passes (e.g. `mypy`/`pyright`, `tsc`, `go vet`); no skipped or disabled existing tests. |
| **G1 — Spec conformance & interop** | Implements the relevant [spec](README.md) section idiomatically; passes the language-neutral [conformance fixtures](conformance/README.md); cross-language interop is verified for anything that crosses the wire (send/receive the envelope against another port). |
| **G2 — Idiomatic, layered packaging** | Follows the language's idioms; ships as layered, install-what-you-use packages named as close to the .NET layering as the language allows (see each port's package doc). A lower layer never imports a higher one. |
| **G3 — A running multi-transport example per cloud provider** | For every cloud provider the port supports, a **fully-runnable** example lives in the port's `examples/` folder and exercises **multiple transports** (see the matrix in §3), including at least one **egress/outbound** path so ingress → handler → egress is demonstrated end to end. |
| **G4 — Example tests that dogfood the port's own test helpers** | Each example ships its own tests, written against the **port's own test-helper packages** (§4) — not a bespoke harness. The example is simultaneously a demo *and* the proof that the test helpers work. |
| **G5 — CI: examples build and all tests pass** | A GitHub Actions workflow builds **every** example and runs **every** test (library + example) on each push and PR, as a **required** check. See §5. |
| **G6 — Documentation updated & website-ready** | Docs are updated as part of the same unit of work, reachable from the docs index, with all links resolving and every snippet copy-paste-runnable. Docs are a **prerequisite** — the work is not done until it is documented, because the website publishes from `docs/`. |
| **G7 — DX champion sign-off** | The §1 loop has completed: the champion planned it, reviewed it, and signed off after feedback was actioned. |

## 3. Per-provider multi-transport example matrix

An example proves the "write once, host anywhere" promise only if it runs the *same* handlers behind
*several* transports on the provider. The reference topology is the .NET `examples/Aws` (multiple
event sources in one Lambda) and `examples/AzureFunctionsMesh` (Service Bus + Event Hub + Event Grid).

| Provider | Host | Transports the example MUST exercise | Egress (outbound) |
|---|---|---|---|
| **AWS** | Lambda | API Gateway (HTTP) **+ SQS + SNS** (SHOULD also show EventBridge and/or Kafka) | publish via an SNS/SQS/EventBridge outbound client |
| **Azure** | Functions | HTTP **+ Service Bus + Event Hub** (SHOULD also show Event Grid) | publish via a Service Bus/Event Grid outbound client |
| **GCP** | Cloud Functions | HTTP **+ Pub/Sub** | publish via a Pub/Sub outbound client |

Minimum bar for any provider: **HTTP inbound + at least two messaging transports (one queue-shaped,
one pub/sub-shaped) + one outbound path.** The same domain handlers MUST be reused across the
transports (mirror the shared-domain pattern of .NET's `examples/App`) — a transport is wiring, not a
rewrite.

## 4. The dogfooding test-helper standard

Benzene's own testing story is the reference (`Benzene.Testing` + the per-transport `*.TestHelpers`
packages). Each port MUST provide the equivalent, and its examples MUST use it. This is *dogfooding*:
we test the examples with the very helpers we ask adopters to use, so the helpers are proven on real
code.

**What each port MUST ship** (idiomatic to the language):

1. **An in-memory test host** built from the application's own start-up/registration, with a seam to
   **override the external edges with fakes** (replace a real client with a fake after the app's own
   registration — last-registration-wins). This is the port's analog of
   `BenzeneTestHost.Create<StartUp>().WithServices(...).Build<Host>(...)`.
2. **Native-event builders** per transport, to construct the raw event a real trigger would deliver
   (an API-Gateway request, an SNS/SQS record, a Pub/Sub push) so a test can drive the host exactly
   as the cloud would — the analog of `ApiGatewayProxyRequestBuilder`, the SNS/SQS event builders.
3. **A Benzene message builder** (topic + typed body + headers) for the transport-neutral envelope.

**The .NET shape to mirror** (translate to the port's idiom):

```csharp
// Build an in-memory host from the example's StartUp, faking only the outbound edge:
var fakeSender = new FakeBenzeneMessageSender();
var entryPoint = BenzeneTestHost.Create<StartUp>()
    .WithServices(services => services.AddSingleton<IBenzeneMessageSender>(fakeSender))
    .BuildAwsLambdaHost();
using var host = new AwsLambdaBenzeneTestHost(entryPoint);

// Drive it with a native transport event and assert the response AND the egress:
var request = new ApiGatewayProxyRequestBuilder("POST", "/orders/publish-created")
    .WithBody(orderCreated).Build();
var response = await host.SendEventAsync<APIGatewayProxyResponse>(request);

Assert.Equal(202, response.StatusCode);
Assert.Equal(MessageTopicNames.OrderCreated, fakeSender.LastTopic);   // ingress -> handler -> egress
```

**Properties the example tests MUST have:** in-memory (no cloud, no network), fast, deterministic; one
test per transport ingress path plus the egress assertion; the external edges are the only fakes (the
pipeline, routing, and handlers are the real thing).

## 5. The CI gate

Examples MUST be part of the build pipeline — this closes the gap where example code compiles "by
looking wired" but is never gate-checked. A GitHub Actions workflow MUST, on every push and PR:

- **Tier 1 — required, credential-free (blocks merge):** build every example and run every example
  test (the in-memory, dogfooded tests of §4). No cloud credentials needed; runs on every PR across
  the port's supported language versions.
- **Tier 2 — real end-to-end (SHOULD, may be scheduled/manual):** deploy each provider's example to
  the real cloud and smoke-test it (the analog of the .NET `deploy-*-example.yml` workflows). Proves
  the example genuinely runs end to end. Not required to block every PR, but each supported provider
  SHOULD have one and it MUST be green before a release.

A red Tier-1 check is a stop-the-line: the unit of work is not done.

## 6. Documentation

Documentation is updated **in the same unit of work**, never deferred:

- The appropriate level(s) are written or updated — getting-started, reference, and/or cookbook —
  following the port's `documentation-writer` guidance.
- Every new doc is reachable from the port's docs index and every internal link resolves.
- Every snippet is copy-paste-runnable against the real, published API (no invented symbols).
- Because the **website publishes from `docs/`**, undocumented work is, by definition, not done —
  documentation is a prerequisite for the feature, not a follow-up.

## 7. Definition-of-Done checklist (copy into each unit of work)

```
Planning
- [ ] DX champion engaged on the plan (package/API/idioms/transports) before build started

Code
- [ ] G0  Builds; linter/type-checker green; no existing tests skipped/disabled
- [ ] G1  Spec section implemented idiomatically; conformance fixtures pass; wire interop verified
- [ ] G2  Idiomatic, layered, install-what-you-use packaging; names close to .NET; deps point down

Examples & tests
- [ ] G3  A runnable multi-transport example per supported provider (HTTP + 2 messaging + egress)
- [ ] G4  Example tests dogfood the port's own test-helper packages (in-memory, edges faked only)
- [ ] G5  CI builds every example and runs every test as a required check (Tier 1); Tier 2 where applicable

Docs & sign-off
- [ ] G6  Docs updated, reachable from the index, links resolve, snippets runnable (website-ready)
- [ ] G7  DX champion reviewed the finished work; all feedback actioned in a cycle; sign-off recorded
```

## Reference implementation

The .NET repository is the worked reference for all of the above: `Benzene.Testing` +
`src/*.TestHelpers` (§4), `examples/Aws` and `examples/AzureFunctionsMesh` (§3), the
`deploy-*-example.yml` workflows (§5 Tier 2), and the `docs/` tree the website publishes (§6).
