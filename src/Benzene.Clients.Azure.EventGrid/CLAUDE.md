# Benzene.Clients.Azure.EventGrid

## What this package does
Outbound Azure Event Grid client for a Benzene app: publish an event to an Event Grid topic. The
egress counterpart of `Benzene.Azure.Function.EventGrid` (release plan Tier 2.2, §5.2). This package
introduces `Azure.Messaging.EventGrid` (5.0.0) fresh — the ingress package is deliberately SDK-free
(it hand-parses the trigger payload), but egress genuinely needs the publisher client.

## Two schemas, both supported (per the release plan)
- **CloudEvents 1.0 (default, recommended)** — `EventGridContextConverter<T>` /
  `OutboundEventGridContextConverter` / `.UseEventGrid(source, ...)`. Builds a `CloudEvent` with
  `Type` set to the Benzene topic. This is the schema Benzene's Event Grid ingress prefers when
  parsing (`EventGridTriggerEvent.Parse` detects CloudEvents via the `specversion` field).
- **Classic Event Grid schema (for existing EG-schema publishers)** —
  `EventGridEventSchemaContextConverter<T>` / `OutboundEventGridEventSchemaContextConverter` /
  `.UseEventGridEventSchema(...)`. Builds an `EventGridEvent` with both `Subject` and `EventType`
  set to the Benzene topic. Prefer the CloudEvents path for new code.
- Both share `EventGridSendMessageContext` (holds exactly one of `CloudEvent`/`EventGridEvent`) and
  `EventGridClientMiddleware` (dispatches to whichever is set).
- `EventGridBatchMessageClient` — `IBenzeneBatchMessageClient` (from `Benzene.Clients`); publishes a
  collection via `SendEventsAsync(IEnumerable<CloudEvent>)` (CloudEvents path). Reuses
  `EventGridContextConverter<T>` per event and chunks with `BatchSend.Chunk` to `batchSize` (default
  `MaxBatchSize` = 100; the Event Grid ~1 MB request cap still applies). **Failure granularity is
  per-chunk** (atomic send): a throwing `SendEventsAsync` reports every event in that chunk as failed
  against its request index in the `BatchSendResult`. Covered by
  `test/Benzene.Core.Test/Clients/Azure/BatchMessageClientTest.cs`.

## Routing — matches the ingress exactly
Benzene's Event Grid ingress (`EventGridMessageTopicGetter`) routes on the event's **type**, not its
subject or source: the CloudEvent `Type` / classic-schema `EventType`. This package sets exactly that
field to the Benzene topic on both paths.

## Headers — honestly scoped, do not assume parity with Service Bus/Event Hubs
- **CloudEvents path**: `ExtensionAttributes` is the CloudEvents-spec mechanism for custom metadata,
  and `EventGridContextConverter<T>` forwards headers there. **However, Benzene's Event Grid ingress
  does not currently read `ExtensionAttributes` back into message headers** (unlike the
  application-property forwarding on Service Bus/Event Hubs) — a same-stack Benzene handler will not
  see correlation id / `traceparent` set this way. The attributes are still set because they're
  correct per the CloudEvents spec and visible to any CloudEvents-compliant subscriber outside
  Benzene; just don't rely on round-tripping them through Benzene's own ingress yet.
- **Header keys are lowercased.** The CloudEvents spec requires extension attribute names to be
  lower-case ASCII letters/digits only — the SDK throws `ArgumentException` at send time otherwise,
  which is how this was caught (`EventGridContextConverterTest`). Both converters call
  `header.Key.ToLowerInvariant()` before setting the attribute, so e.g. the default correlation-id
  header key `"correlationId"` becomes the attribute `"correlationid"`. Two headers differing only
  by case would collide under this scheme (an accepted, documented edge case, not silently ignored).
- **Classic schema path**: there is no header bag at all on an `EventGridEvent` — headers are not
  forwarded, full stop.

## No `TokenCredential`/connection-string wrapping — deliberately
This package takes an already-built `EventGridPublisherClient`, not a connection string or
`TokenCredential`. `EventGridPublisherClient` itself supports a `TokenCredential` constructor
overload (for Managed Identity) — that choice is the caller's, made when constructing the client,
consistent with every other egress package in this family.

## No health check — deliberately (no cheap non-destructive reachability read exists)
Unlike the other broker send-clients (SQS/SNS/Queue Storage/Event Hub), Event Grid ships **no** health
check. `EventGridPublisherClient` is **publish-only** — the data plane has no describe/get-properties
call to probe. The alternatives are all worse than nothing: a **management-plane** `GetTopic` needs a
whole new SDK + `Manage` credentials the publisher doesn't have; a **synthetic publish** is
side-effecting (it fans out to real subscribers), so it could only ever be an opt-in `Active` check,
never the non-destructive default; a bare TCP/DNS reach of the endpoint proves almost nothing (shared
Azure front-end). If a real need appears, the right shape is an explicit opt-in `Active`
synthetic-publish check pointed at a dedicated no-subscriber probe topic, with the ⚠️ side-effecting
treatment — not an auto-wired default. See `work/client-health-checks-remaining-designs.md` §4.

## Dependencies
`Azure.Messaging.EventGrid`; Benzene `Clients`, `Core.Middleware`, `Results`.
