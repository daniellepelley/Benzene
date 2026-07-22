# Benzene.Clients.Aws.EventBridge

## What this package does
Outbound EventBridge client for a Benzene app: put events on an EventBridge bus. Pins **only**
`AWSSDK.EventBridge`.

## Key types
- `EventBridgeBenzeneMessageClient` — `IBenzeneMessageClient`; puts events on a bus.
- `EventBridgeClientMiddleware` / `EventBridgeSendMessageContext` — terminal put-events middleware
  and its context.
- `EventBridgeContextConverter<T>` — `IBenzeneClientContext<T, Void>` → put-events context.
- `EventBridgeBatchMessageClient` — `IBenzeneBatchMessageClient` (from `Benzene.Clients`); puts a
  collection via `PutEvents` (≤10/call). Reuses `EventBridgeContextConverter<T>` per entry, chunks
  with `BatchSend.Chunk`, and maps failures back to caller indices in a `BatchSendResult`. Unlike
  SQS/SNS there is no per-entry id — `PutEvents` responds positionally (`Entries[i]` ↔ request
  entry `i`, a failed entry carries `ErrorCode`), so failures are mapped by position within each
  chunk. Covered by `test/Benzene.Core.Test/Clients/Aws/BatchMessageClientTest.cs`.
- `Extensions` — `UseEventBridgeClient`, `UseEventBridge<T>` (a `source` overload and a
  pipeline-configuring overload), the **`OutboundContext`** `UseEventBridge(source, eventBusName?, …)`
  overloads (below), and **`AddEventBridgeHealthCheck`**.
- `OutboundEventBridgeContextConverter` — `OutboundContext` → `EventBridgeSendMessageContext`, so an
  outbound route (`OutboundRoutingBuilder.Route`) can publish to EventBridge the same way `.UseSqs(...)`/
  `.UseSns(...)` do (the EventBridge twin of `OutboundSns/SqsContextConverter`, added 2026-07-22). Maps the
  Benzene topic → `DetailType` and the configured `source` → `Source`; embeds wire headers into `Detail`
  under `_benzeneHeaders` exactly as `EventBridgeContextConverter<T>` does (shares its `EmbeddedHeadersKey`).
  Fire-and-acknowledge, so the response is always `IBenzeneResult<Void>` — route a topic here and send it
  via `IBenzeneMessageSender.SendAsync<TRequest,Void>`. The `UseEventBridge(this
  IMiddlewarePipelineBuilder<OutboundContext>, string source, string? eventBusName = null, bool healthCheck
  = true)` overload auto-registers the reachability check for `eventBusName` (null = default bus) on the
  dependency category, mirroring `.UseSns(...)`.
- `EventBridgeHealthCheck` — verifies an event bus with a read-only `DescribeEventBus` call
  (`Type = "EventBridge"`, dependency `("EventBus", name)`; null name = the `"default"` bus). Failures
  are classified via `HealthCheckError.Classify` (§3.9): a permission error (403) is a **Warning**, and
  the SDK `ErrorCode`/`StatusCode` are surfaced in `Data`, never the exception message.
  - **Auto-wired (Phase 4, default-on).** The `source` overload of `UseEventBridge<T>` takes
    `bool healthCheck = true`: unless opted out it auto-registers the check for the **default bus** on the
    **dependency category** (`AddDependencyHealthCheck`, dedup `"EventBridge:default"`), reusing the
    `IAmazonEventBridge` from DI. Deep `healthcheck` layer only — never a probe (shared-fate; see
    `IDependencyHealthCheck`). The pipeline-configuring overload doesn't auto-wire.

## Conventions
- EventBridge routing is driven by the event's `Source`/`DetailType`, not a message attribute — the
  converter maps the Benzene message onto the `PutEventsRequestEntry` accordingly.
- Like the other AWS senders, the outbound path is a fire-and-acknowledge (`IBenzeneResult<Void>`);
  there is no request/response.
- The health check is **reachability-only** (`DescribeEventBus`) — there is no `Active` mode, because a
  "publish a real event" probe would fire live rules/targets. `DescribeEventBus` proves reachability +
  `events:DescribeEventBus`, not that `events:PutEvents` would succeed (the usual reachability tradeoff).

## Dependencies
`AWSSDK.EventBridge`; Benzene `Clients`, `Core.Middleware`, `Results`.
