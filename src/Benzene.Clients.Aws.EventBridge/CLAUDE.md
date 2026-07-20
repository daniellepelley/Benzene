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
  pipeline-configuring overload).

## Conventions
- EventBridge routing is driven by the event's `Source`/`DetailType`, not a message attribute — the
  converter maps the Benzene message onto the `PutEventsRequestEntry` accordingly.
- Like the other AWS senders, the outbound path is a fire-and-acknowledge (`IBenzeneResult<Void>`);
  there is no request/response.
- **No health check** — EventBridge has no cheap non-mutating liveness probe.

## Dependencies
`AWSSDK.EventBridge`; Benzene `Clients`, `Core.Middleware`, `Results`.
