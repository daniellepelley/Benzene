# AWS Mesh — multi-transport dogfooding plan

## Goal
Grow `examples/AwsMesh` from a 3-service, SQS-only chain into a richer topology of services that call
each other over **SQS, SNS and EventBridge** — each transport used for the job it's actually good at,
with a real sender client on one side and the matching Lambda event-source handler on the other. The
point is to dogfood Benzene's "one set of handlers, hosted anywhere / messaged over anything" promise
on a live AWS deploy, and to give the Mesh UI a topology worth looking at.

A follow-up (separate) initiative does the same for Azure (Service Bus / Event Hub / Event Grid) — see
the last section.

## Where we are today
- **3 services** (`orders`, `payments`, `shipping`) + the `mesh` Lambda, discovered by tag.
- Every service already exposes its handlers over **5 ingress** surfaces (API Gateway, direct
  BenzeneMessage invoke, SQS, SNS, EventBridge) via `Shared/MeshServiceWiring`.
- **But the only real inter-service traffic is SQS.** `MeshServiceWiring.ConfigureServices` hardwires
  every outbound send to `pipeline.UseSqs(queueUrl)`; the chain is `orders --SQS--> payments --SQS-->
  shipping`. SNS/EventBridge ingress is only ever exercised by hand from the Lambda test tool, never by
  a service actually publishing to them.
- Request/response over **direct Lambda invoke** *is* already dogfooded — the mesh interrogates each
  service (`spec`/`healthcheck`) that way. So async messaging (SQS/SNS/EventBridge) is the real gap.

## Design principle: use each transport for its strength
The demo is most convincing (and most instructive) if each transport does what it's idiomatically for,
rather than three interchangeable ways to move a byte array:

| Transport | Idiomatic use | In the demo |
|---|---|---|
| **SQS** | point-to-point **command**, one consumer, durable, retry/DLQ | `orders → payments` (`payments:capture`), `payments → shipping` (`shipping:book`) |
| **SNS** | pub/sub **event**, one publisher → **many** subscribers (fan-out) | `orders` publishes `order:placed` → **inventory** *and* **notifications** |
| **EventBridge** | routed **integration events**, content rules, one event → many targets | `payments` publishes `payment:captured`, `shipping` publishes `shipping:dispatched` → **notifications / inventory / analytics** via rules |

## Target topology (6 services, up from 3)
```
                       API Gateway (per service)  +  direct-invoke (mesh interrogation)

  ┌─────────┐  SQS: payments:capture   ┌──────────┐  SQS: shipping:book   ┌──────────┐
  │ orders  │ ───────────────────────► │ payments │ ───────────────────► │ shipping │
  └────┬────┘                          └────┬─────┘                       └────┬─────┘
       │ SNS: order:placed (fan-out)        │ EventBridge: payment:captured    │ EventBridge:
       │                                    │                                  │ shipping:dispatched
       ▼            ▼                       ▼            ▼                      ▼        ▼        ▼
  ┌──────────┐ ┌───────────────┐      ┌───────────────┐ ┌───────────┐   (inventory)(notifications)(analytics)
  │inventory │ │ notifications │      │ notifications │ │ analytics │
  └──────────┘ └───────────────┘      └───────────────┘ └───────────┘
   reserve stock  notify customer       notify customer   metrics
```
- **orders** (existing) — on `orders:create`: send `payments:capture` (**SQS**, command) **and** publish
  `order:placed` (**SNS**, event).
- **payments** (existing) — on `payments:capture`: send `shipping:book` (**SQS**) **and** publish
  `payment:captured` (**EventBridge**).
- **shipping** (existing) — on `shipping:book`: publish `shipping:dispatched` (**EventBridge**).
- **inventory** (NEW) — consumes `order:placed` (SNS → reserve) and `shipping:dispatched` (EventBridge →
  decrement). One service, two different event transports.
- **notifications** (NEW) — consumes `order:placed` (SNS), `payment:captured` + `shipping:dispatched`
  (EventBridge). Proves SNS fan-out (it and inventory both get `order:placed`).
- **analytics** (NEW) — consumes `payment:captured` + `shipping:dispatched` (EventBridge). Proves one
  EventBridge event → multiple rule targets.

Every new service is a **full Cloud Service** (uses `MeshServiceWiring`, carries the `benzene` tag), so
the mesh discovers all six and the Topology view renders the whole graph — structural edges from the
publishers' declared `events`, plus observed edges once Tempo is layered on.

## Framework prerequisite (small, in `src/`): EventBridge on the outbound-routing model
`AddOutboundRouting(...).Route(topic, p => p.UseSqs(...) / p.UseSns(...))` works for SQS and SNS —
those `IMiddlewarePipelineBuilder<OutboundContext>` overloads live in `Benzene.Clients.Aws.{Sqs,Sns}`.
**EventBridge has no such overload** — its only egress is the older `IBenzeneClientContext<T,Void>`
client model (`UseEventBridge<T>(source, …)`). So today you can't uniformly route an outbound topic to
EventBridge the way you can to SQS/SNS. Dogfooding surfaced the gap; close it:

- Add `Benzene.Clients.Aws.EventBridge`:
  - `OutboundEventBridgeContextConverter` — `OutboundContext` → `EventBridgeSendMessageContext`, mapping
    `OutboundContext.Topic` → `DetailType` (what the EventBridge **ingress** reads back as the topic) and
    a configured `source` → `Source`, honoring `Headers`. Mirror `OutboundSnsContextConverter`.
  - `UseEventBridge(this IMiddlewarePipelineBuilder<OutboundContext> app, string source, string? busName =
    null, bool healthCheck = true)` — mirror `UseSns`, reusing the existing `EventBridgeClientMiddleware`
    and the default-on dependency health check (targeting `busName`).
- Unit test mirroring `Aws/Sns/OutboundSnsContextConverterTest.cs`: routed end-to-end through
  `AddOutboundRouting` + resolved `IBenzeneMessageSender`, asserting the mocked `IAmazonEventBridge`
  receives the right bus, `DetailType == topic`, `Source`, serialized detail, and forwarded headers.
- **Non-breaking** (new API only). Restores SQS/SNS/EventBridge parity on the outbound routing model.
  CHANGELOG under `### Added`; update `Benzene.Clients.Aws.EventBridge/CLAUDE.md` and the `Benzene.Clients`
  note that currently says only `.UseSqs`/`.UseSns` exist (and that only `.UseAwsLambda` remains deferred).

Note: `IBenzeneMessageSender` request/response is **ack-only** for SQS/SNS/EventBridge (a non-`Void`
`TResponse` throws `OutboundResponseTypeMismatchException`), so all six services' inter-service sends are
fire-and-forget `SendAsync<T, Void>` — consistent with the existing chain. No request/response over these
transports is attempted (that path stays the mesh's direct-invoke interrogation).

## Example changes (`examples/AwsMesh`)
1. **Generalize the send descriptor + wiring** (`Shared/OutboundSend.cs`, `Shared/MeshServiceWiring.cs`):
   - `OutboundSend(string Topic, Type MessageType, OutboundTransport Transport, string TargetEnvVar)` where
     `OutboundTransport ∈ { Sqs, Sns, EventBridge }`; `TargetEnvVar` holds the queue URL / topic ARN / bus
     name+source as appropriate.
   - In `ConfigureServices`, replace the hardcoded `UseSqs(queueUrl)` with a switch → `UseSqs` / `UseSns` /
     `UseEventBridge`. Register `IAmazonSNS` / `IAmazonEventBridge` lazily alongside the existing lazy
     `IAmazonSQS`, only when a send of that transport is present. The `ResponseEventDefinition` declaration
     (→ structural topology edge) stays transport-agnostic and unchanged, so every edge still shows up in
     the mesh regardless of transport.
   - Convert the two existing hops to the generalized model with `Transport.Sqs` — **no behavior change**,
     proves the mechanism against the current 3 services before adding any.
2. **New service projects** `Inventory/`, `Notifications/`, `Analytics/` — mirror `Orders/` structure
   (`Program.cs`, `Startup.cs` calling `MeshServiceWiring`, `Handlers/`, `Model/`, `HealthChecks/`,
   `Validators/`, `.csproj`, `.lambda-test-tool/SavedRequests/`). Consumer services have no HTTP domain
   routes to speak of — just the event handlers (`[Message("order:placed")]`, etc.) plus the Cloud Service
   Profile the shared wiring always adds. Add the new publish `OutboundSend`s to orders/payments/shipping.
3. **`Benzene.Examples.sln`** — add the three new projects (allowed for the examples solution; do not touch
   `Benzene.sln`). Build via `Benzene.Examples.sln`, since examples aren't in the main CI gate.

## Infra (`examples/AwsMesh/deploy`, Terraform)
The existing `for_each = local.services` map already fans out Lambda + IAM + API Gateway per service — add
the three new entries there. New messaging infra:
- **SNS** — one topic `order:placed`; SNS→Lambda subscriptions for `inventory` and `notifications` (+
  `aws_lambda_permission` allowing SNS to invoke each); `sns:Publish` on the topic for the `orders` role;
  the topic ARN handed to `orders` via env var.
- **EventBridge** — a custom bus `${var.project}-bus` (isolation from default-bus account noise); rules
  matching `detail-type = payment:captured` → {notifications, analytics}, `detail-type =
  shipping:dispatched` → {inventory, notifications, analytics}; Lambda targets + `aws_lambda_permission`
  per target; `events:PutEvents` on the bus for the `payments`/`shipping` roles; bus name + source handed
  to publishers via env vars. (Keep the existing default-bus `mesh:aggregate` schedule as-is.)
- **SQS** — unchanged (payments/shipping queues, mappings, IAM).
- Model the messaging edges explicitly rather than over-abstracting the `for_each` — the wiring is
  heterogeneous and reads clearer flat. Expect ~+250 lines in `main.tf`.

## Test tool, README, verification
- Add `.lambda-test-tool/SavedRequests/*.json` for the new topics/services (e.g. `inventory-order-placed-
  sns.json`, `notifications-payment-captured-eventbridge.json`).
- Rewrite `README.md`: new architecture diagram, the transport-per-strength table, the fan-out story, and
  "see the chain fire" updated to trace `orders:create` → SQS + SNS + EventBridge across six services in
  CloudWatch.
- **Verify:** `dotnet build Benzene.sln` (the `src/` EventBridge addition) + its new unit test; `dotnet
  build Benzene.Examples.sln` (all example projects). Live AWS behavior is only provable on a real deploy —
  run the **Deploy AWS Mesh Example** workflow, then open the Mesh UI and confirm the six-node topology.

## Suggested PR slices (reviewable, each green on its own)
- **PR 1 — framework:** `UseEventBridge` on `OutboundContext` + converter + unit test + docs. `src/` only.
- **PR 2 — example egress generalization:** `OutboundSend`/`MeshServiceWiring` transport switch; port the
  two existing SQS hops (no behavior change) + add the SNS/EventBridge publishes from orders/payments/
  shipping. Still 3 services.
- **PR 3 — new services:** inventory, notifications, analytics projects + handlers + saved requests.
- **PR 4 — infra + docs:** Terraform SNS/EventBridge/new-Lambda wiring; README rewrite + diagram.
- **PR 5 (optional stretch):** transport-labeled topology edges in the mesh contract/UI (an edge showing
  *which* transport carries it). Mesh-contract change — separate, only if wanted.

## Open decisions (for approval before I start)
1. **Domain/topology** — the six-service e-commerce fulfillment shape above, or a different domain?
2. **EventBridge bus** — custom `${project}-bus` (recommended, isolated) vs the default bus (least infra).
3. **Scope now** — all three new services in one go, or land SNS fan-out (inventory + notifications) first
   and add EventBridge/analytics second?
4. **Framework addition** — OK to add `UseEventBridge` on `OutboundContext` (PR 1)? Strongly recommended;
   without it EventBridge egress can't use the same routing model as SQS/SNS.

## Next (separate initiative): Azure counterpart
Repeat the exercise on `examples/AzureFunctionsMesh` / `examples/AzureMesh` with the Azure transports —
**Service Bus** (point-to-point commands), **Event Hub** (streaming/fan-out), **Event Grid** (routed
integration events) — reusing the same generalized `MeshServiceWiring` idea. Planned after the AWS work
lands so the pattern is proven once first.
