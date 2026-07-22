# AzureFunctionsMesh — multi-transport dogfooding plan

## Goal
The Azure counterpart of `work/aws-mesh-multi-transport-plan.md`: make the `examples/AzureFunctionsMesh`
services call each other over **Service Bus, Event Hub and Event Grid**, each used for what it's good at,
so the Mesh UI topology renders a real graph on Azure Functions too.

## Good news — no framework work needed
Unlike AWS (where EventBridge lacked an `OutboundContext` egress and needed a `src/` change), Azure is
already complete on both sides:
- **Egress:** `UseServiceBus`, `UseEventHub`, `UseEventGrid` (and `UseQueueStorage`) are all
  `IMiddlewarePipelineBuilder<OutboundContext>` overloads in `Benzene.Clients.Azure.*`.
- **Ingress:** every trigger attribute exists for the source generator — `BenzeneServiceBusTrigger`,
  `BenzeneEventHubTrigger`, `BenzeneEventGridTrigger`, etc.

So the entire effort is in `examples/AzureFunctionsMesh` + its Terraform. 

## Where we are today
- **One** `Service` project, deployed **3×** as Function Apps (`orders`/`payments`/`shipping`); the
  `MESH_SERVICE` app setting selects which domain handler is registered (`Domain.HandlersFor`).
- **HTTP-only** — the only trigger is the source-generated `[assembly: BenzeneHttpTrigger]`.
- **No inter-service calls at all** — handlers just return a result; there's no `IBenzeneMessageSender`.
- Terraform deploys the services via `for_each = toset(["orders","payments","shipping"])`, each Function
  App differing only by its `MESH_SERVICE` setting; the mesh discovers them by tag.

## Transport-per-strength mapping (the Azure equivalents)
| Azure transport | Idiomatic for | AWS analogue | In the demo |
|---|---|---|---|
| **Service Bus queue** | point-to-point **command**, one consumer, sessions/DLQ | SQS | `orders → payments` (`payment:take`), `payments → shipping` (`shipment:book`) |
| **Event Hub** | high-throughput **event stream**, fan-out via consumer groups | (Kinesis/SNS) | `orders` streams `order:placed` → **inventory + notifications** (a consumer group each) |
| **Event Grid** | **routed discrete events**, subject/type filters, many subscribers | EventBridge/SNS | `payments` publishes `payment:captured`, `shipping` publishes `shipment:dispatched` → **notifications / inventory / analytics** |

## The architecture decision (needs your call)
Azure Functions triggers are **assembly-level** — one set of `[assembly: Benzene…Trigger]` declarations
compiled into the single `Service` assembly, shared by every deployment of it. That collides with
"different services consume different transports". Two ways to resolve it:

- **Option A — keep the single parameterized `Service` (faithful to today's design / K8sMesh).**
  Declare *all* the triggers (HTTP + Service Bus + Event Hub + Event Grid) in the one assembly, each
  bound to an entity named by an app setting (`%SERVICE_BUS_QUEUE%`, `%EVENT_HUB_NAME%`, …). Per
  deployment, set the entity names for the transports that service consumes and **disable the rest** via
  `AzureWebJobs.<function>.Disabled=true`. One deployable, an app-settings matrix per service.
  - *Pro:* preserves the deliberate one-image design; no `.sln`/project churn.
  - *Con:* the enable/disable + entity-name matrix is fiddly and less obvious than per-service wiring;
    every service still *carries* (disabled) triggers it doesn't use.
- **Option B — split into per-service (or per-role) projects, like AwsMesh.**
  Each service is its own project declaring exactly the triggers it uses. Clean, obvious wiring —
  this is *why* AwsMesh used per-service projects once transports diverged.
  - *Pro:* each service's transports are explicit and self-contained; matches AwsMesh 1:1.
  - *Con:* a real restructure of an existing example (new projects in the `.sln`, Terraform `for_each`
    over a richer map, deploy workflow), and it walks back the single-image choice the example made.

**Recommendation: Option A**, because it keeps the example's existing single-deployable philosophy and
avoids restructuring — the app-settings matrix is contained in Terraform. But Option B is the cleaner
long-term shape if you'd rather this mirror AwsMesh exactly.

## Scope decision (needs your call)
- **Match AwsMesh's 6-service topology** — add `inventory`/`notifications`/`analytics` domains to
  `Domain.HandlersFor`, so the Azure mesh graph looks like the AWS one. (Also mirrors into
  `K8sMesh/Service/Domain.cs`, which this file is kept in sync with — a small extra edit.)
- **or keep the existing 3** and just interconnect orders→payments→shipping over Service Bus, with the
  event transports demonstrated more minimally.

**Recommendation: match the 6-service topology**, for parity with AWS and a richer mesh graph.

## Implementation outline (once A/B + scope are settled)
Assuming **Option A + 6 services**:
1. **Domain** (`Service/Domain.cs`): add `inventory`/`notifications`/`analytics` domains + their event
   handlers (`IMessageHandler<TRequest>`), and add outbound sends to orders/payments/shipping handlers
   via `IBenzeneMessageSender` (Service Bus command + Event Hub stream + Event Grid publish).
2. **Egress wiring** (`Service/StartUp.cs`): `AddOutboundRouting` with `UseServiceBus`/`UseEventHub`/
   `UseEventGrid` per topic, targets from app settings; register the Azure SDK clients.
3. **Ingress triggers** (`Service/Triggers.cs`): add `[assembly: BenzeneServiceBusTrigger]`,
   `BenzeneEventHubTrigger`, `BenzeneEventGridTrigger` bound to `%…%` app settings; wire the matching
   `UseServiceBus`/`UseEventHub`/`UseEventGrid` ingress in `StartUp.Configure`.
4. **Terraform** (`deploy/`): a Service Bus namespace + queues, an Event Hub namespace + hub +
   consumer groups, Event Grid topic(s) + subscriptions, the per-service app-settings matrix (entity
   names + `Disabled` flags + connection strings), and the send/listen role assignments (managed identity).
5. **README** + `local.settings.json` updates; build the `.sln`.

## Verification reality
Same as AWS: I can make it build (`Benzene.Example.AzureFunctionsMesh.sln`), but the live Azure behaviour
(trigger binding, Event Hub consumer-group fan-out, Event Grid subscription routing, identity role
assignments) is only provable by running the **Deploy Azure Functions Mesh Example** workflow.
