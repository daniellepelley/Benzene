# Benzene.HealthChecks.Azure.ServiceBus

## What this package does
A single `IHealthCheck` (`ServiceBusHealthCheck`) that verifies an Azure Service Bus entity (a queue,
or a topic subscription) is reachable with a read-only `PeekMessage` call. First of the Azure-transport
health checks (Service Bus / Event Hub / Queue Storage / Event Grid previously had none). A dedicated
check-only package referencing `Azure.Messaging.ServiceBus` + `Benzene.HealthChecks.Core`.

## Key types
- `ServiceBusHealthCheck` - two ctors: `(ServiceBusClient, queueName)` and
  `(ServiceBusClient, topicName, subscriptionName)`. `Type => "ServiceBus"`; `Data` = Entity (+ the
  namespace host if the client exposes one, + `Error` = exception **type name** on failure);
  `Dependencies` = one `HealthCheckDependency("Queue", queueName)` or
  `("Subscription", "topic/subscription")`.
- `ServiceBusHealthCheckFactory` - builds the check for a fixed queue/subscription, resolving
  `ServiceBusClient` from DI.
- `Extensions.AddServiceBusQueueHealthCheck(builder, queueName)` /
  `AddServiceBusSubscriptionHealthCheck(builder, topicName, subscriptionName)` - registration helpers.

## Why `PeekMessage` (not send, receive, or a management call)
- **Non-side-effecting**: peek neither locks, completes, nor removes a message (unlike a real receive)
  and sends nothing (unlike `SqsHealthCheck`/`StepFunctionsHealthCheck`, which mutate the dependency
  every probe). It returns `null` on an empty entity, so the round-trip alone is the connectivity signal.
- **Data-plane only**: needs just the `Listen` claim a consumer already holds - not the management-plane
  `Manage` claim `ServiceBusAdministrationClient.GetQueueRuntimePropertiesAsync` would require, which
  data-plane identities usually lack.
- Cost: a short-lived `ServiceBusReceiver` AMQP link is opened and disposed per probe. Cheaper than the
  side-effecting AWS checks, but not free - mind the readiness cadence, and rely on the aggregator's
  `TimeOutHealthCheck` wrapper (this check has no independent timeout).

## Conventions
- `ServiceBusClient` is resolved from DI; the **consumer** registers it - Benzene never wraps the
  client's authentication choice (connection string, `DefaultAzureCredential`, emulator), mirroring the
  `IServiceBusClientFactory` seam in `Benzene.Azure.ServiceBus` and the "already-built client" stance of
  `Benzene.Clients.Azure.ServiceBus`.
- Failures are classified via `HealthCheckError.Classify` (§3.9, reversed): a bad credential/claim
  (`UnauthorizedAccessException`, how Azure Messaging signals no `Listen`) is a **persistent `Failed`**
  (mapped to 403) — surfacing as unhealthy rather than being softened to a Warning, since a missing
  claim is a deterministic misconfiguration that won't self-heal — else a transient Failed; the SDK's
  `ServiceBusException.Reason` is reported as `ErrorCode`. Always the
  exception **type name** / reason, never its message - no connection string / entity secret reaches
  `Data`. Auto-wired from the consumer via `Benzene.Azure.ServiceBus`'s `UseServiceBus(..., healthCheck:)`.

## Dependencies
`Azure.Messaging.ServiceBus` (7.18.2, matching the other Azure Service Bus packages); Benzene
`HealthChecks.Core`.
