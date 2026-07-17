# Managed Identity & RBAC for Azure Resources

Run every Benzene Azure integration — Service Bus, Event Hubs, Cosmos DB, and the Functions
runtime itself — with **no connection strings**: the host authenticates as a Microsoft Entra ID
identity, and access is granted with Azure RBAC roles instead of shared keys.

## Problem Statement

Connection strings are shared secrets: anyone who reads the app setting owns the namespace, they
don't expire, rotating them is a coordinated deploy, and they grant far more than most consumers
need (a listen-only worker holding a key that can also send and manage). Microsoft's guidance —
and most enterprise Azure policy — is to disable shared-key access entirely and use managed
identities with least-privilege RBAC roles.

This cookbook shows how to do that for every Azure transport Benzene ships, in both hosting modes:

- **Self-hosted workers** (`Benzene.Azure.ServiceBus`, `Benzene.Azure.EventHub`,
  `Benzene.Azure.CosmosDb`) — you pass a credential when you build the SDK client.
- **Azure Functions triggers** (`Benzene.Azure.Function.{ServiceBus,EventHub,CosmosDb}`) — no
  code at all; identity-based connections are pure app-settings convention.

## Where Benzene fits (and deliberately doesn't)

Benzene owns **none** of this. Every Azure package exposes a factory seam where *you* build the
SDK client, and authentication is a client-construction concern:

| Package | Seam | You build |
|---|---|---|
| `Benzene.Azure.ServiceBus` | `IServiceBusClientFactory` | `ServiceBusClient` |
| `Benzene.Azure.EventHub` | `IEventProcessorClientFactory` | `EventProcessorClient` (+ its `BlobContainerClient` checkpoint store) |
| `Benzene.Azure.CosmosDb` | `ICosmosChangeFeedProcessorFactory<T>` | `CosmosClient` → `Container`s |
| `Benzene.Azure.Function.*` | none needed | app settings only — the Functions host builds the clients |

So "adding managed identity to Benzene" is just building those clients with a `TokenCredential`
instead of a connection string. Nothing else in your pipeline, handlers, or startup changes.

## Prerequisites

- One of the Azure workers or Functions triggers already wired up (see
  [Worker Service Setup](../getting-started-worker.md) or
  [Azure Functions Setup](../azure-functions.md)).
- `Azure.Identity` referenced by your app for the self-hosted scenarios:

```bash
dotnet add package Azure.Identity
```

(Benzene itself pins `Azure.Identity` 1.11.4 in `Benzene.Azure.Function.Core`; referencing it
directly in your own app keeps you in charge of the version.)

- An identity to grant roles to: a **system-assigned managed identity** on the Function
  App/App Service/Container App/VM (enable it on the resource; Azure creates and rotates the
  credential), a **user-assigned managed identity**, or — on AKS — a **workload identity**.

## `DefaultAzureCredential` in one minute

```csharp
using Azure.Identity;

var credential = new DefaultAzureCredential();
```

`DefaultAzureCredential` tries a chain of sources in order — environment variables (service
principal), workload identity, **managed identity**, and, for local development, your Azure CLI /
Visual Studio login. The same line of code therefore authenticates as the managed identity in
Azure and as *you* on your laptop (`az login`), which is the whole trick: no per-environment
branching, no secrets in either place.

For a **user-assigned** identity, name the client id (otherwise the platform can't know which of
several identities you mean):

```csharp
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = configuration["Azure:ManagedIdentityClientId"]
});
```

Identity-based clients address resources by **fully-qualified hostname or endpoint URI** — e.g.
`my-namespace.servicebus.windows.net` — never by connection string.

## Self-hosted workers

### Service Bus (`Benzene.Azure.ServiceBus`)

Swap the connection-string `ServiceBusClient` for the hostname + credential constructor; the
factory and everything after it are unchanged:

```csharp
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Benzene.Azure.ServiceBus;

var client = new ServiceBusClient(
    "my-namespace.servicebus.windows.net",          // fully-qualified namespace, no scheme
    new DefaultAzureCredential());

app.UseWorker(worker => worker.UseServiceBus(
    new BenzeneServiceBusConfig { QueueName = "orders" },
    new ServiceBusClientFactory(client),
    serviceBus => serviceBus.UseMessageHandlers()));
```

**Roles** (assign at the namespace, or tighter at the queue/topic — RBAC scopes nest):

| Need | Built-in role |
|---|---|
| Consume (what the worker does) | **Azure Service Bus Data Receiver** |
| Send (if you also produce) | **Azure Service Bus Data Sender** |

### Event Hubs (`Benzene.Azure.EventHub`)

Two resources need roles here, because the processor checkpoints to blob storage:

```csharp
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Storage.Blobs;
using Benzene.Azure.EventHub;

var credential = new DefaultAzureCredential();

// Checkpoint store: container URI + credential (container must already exist).
var checkpointStore = new BlobContainerClient(
    new Uri("https://mystorageacct.blob.core.windows.net/eventhub-checkpoints"),
    credential);

var processorClient = new EventProcessorClient(
    checkpointStore,
    EventHubConsumerClient.DefaultConsumerGroupName,
    "my-namespace.servicebus.windows.net",           // fully-qualified namespace
    "telemetry",                                     // event hub name
    credential);

app.UseWorker(worker => worker.UseEventHub(
    new BenzeneEventHubConfig { CheckpointInterval = 100 },
    new EventProcessorClientFactory(processorClient),
    eventHub => eventHub.UseMessageHandlers()));
```

**Roles:**

| Resource | Need | Built-in role |
|---|---|---|
| Event Hubs namespace (or hub) | Consume | **Azure Event Hubs Data Receiver** |
| Storage account (or checkpoint container) | Read/write checkpoints | **Storage Blob Data Contributor** |

### Cosmos DB Change Feed (`Benzene.Azure.CosmosDb`)

```csharp
using Azure.Identity;
using Benzene.Azure.CosmosDb;
using Microsoft.Azure.Cosmos;

var client = new CosmosClient(
    "https://my-account.documents.azure.com:443/",   // account endpoint, not connection string
    new DefaultAzureCredential());

var factory = new CosmosChangeFeedProcessorFactory<OrderDocument>(
    monitoredContainer: client.GetContainer("shop", "orders"),
    leaseContainer: client.GetContainer("shop", "leases"),
    processorName: "orders-projection",
    instanceName: Environment.MachineName);

app.UseWorker(worker => worker.UseCosmosDbChangeFeed(
    new BenzeneCosmosChangeFeedConfig(), factory,
    feed => feed.UseStream<OrderDocument>(/* ... */)));
```

**Roles — Cosmos DB is the odd one out.** Data-plane access is *not* granted through the portal's
IAM blade or normal `az role assignment create`; Cosmos has its own data-plane RBAC with its own
built-in roles and its own CLI command:

| Need | Built-in data-plane role (id) |
|---|---|
| Read only | Cosmos DB Built-in Data Reader (`00000000-0000-0000-0000-000000000001`) |
| Change feed processing (reads the feed **and writes leases**) | **Cosmos DB Built-in Data Contributor** (`00000000-0000-0000-0000-000000000002`) |

```bash
az cosmosdb sql role assignment create \
  --account-name my-account \
  --resource-group my-rg \
  --scope "/" \
  --principal-id <principal-object-id> \
  --role-definition-id 00000000-0000-0000-0000-000000000002
```

The change feed processor needs Contributor, not Reader — it writes lease documents to the lease
container. A 403 from an identity that "definitely has a role in the portal" is almost always
this: an ARM role was granted where a data-plane role was needed.

### Kafka over Event Hubs (`Benzene.Kafka.Core`) — Entra ID yes, managed identity not yet

If you consume Event Hubs through its Kafka-compatible endpoint with the self-hosted Kafka
worker, Entra ID auth replaces the `$ConnectionString` SASL/PLAIN trick with Kafka's
`OAUTHBEARER` mechanism. What works through Benzene today is the **config-only OIDC
client-credentials flow** — an Entra *service principal* (client id + secret), granted the same
RBAC role as any other identity, expressed entirely on the `ConsumerConfig` that
`BenzeneKafkaConfig` already carries:

```csharp
consumerConfig.SecurityProtocol = SecurityProtocol.SaslSsl;
consumerConfig.SaslMechanism = SaslMechanism.OAuthBearer;
consumerConfig.SaslOauthbearerMethod = SaslOauthbearerMethod.Oidc;
consumerConfig.SaslOauthbearerTokenEndpointUrl =
    "https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token";
consumerConfig.SaslOauthbearerClientId = "<app-client-id>";
consumerConfig.SaslOauthbearerClientSecret = configuration["Kafka:ClientSecret"];
consumerConfig.SaslOauthbearerScope = "https://my-namespace.servicebus.windows.net/.default";
```

That's Entra ID + RBAC (the same **Azure Event Hubs Data Receiver** role — the Kafka endpoint is
just a different protocol onto the same namespace), but it is *not* secretless: the client secret
is still a credential you store and rotate. A true managed-identity token
(`ConsumerBuilder.SetOAuthBearerTokenRefreshHandler` feeding tokens from
`DefaultAzureCredential`) is **not reachable through `Benzene.Kafka.Core` today** —
`BenzeneKafkaWorker` builds its `ConsumerBuilder` internally and exposes no hook on it. That's a
known gap (tracked in `work/azure-roadmap-1.0.md`); if you need secretless Kafka now, consume the
namespace through the native path (`Benzene.Azure.EventHub`, above) instead, which the same hub
supports.

## Azure Functions triggers: app settings only

The Functions extensions resolve any trigger's `Connection` property in two ways: a setting named
`X` holding a connection string, or a *group* of settings named `X__property` describing an
identity-based connection. Switching a Benzene Functions app to managed identity is therefore
**zero code** — the `[ServiceBusTrigger(..., Connection = "ServiceBusConnection")]` attribute and
all Benzene wiring stay exactly as they are; only the app settings change:

| Trigger | Replace setting | With |
|---|---|---|
| Service Bus | `ServiceBusConnection` | `ServiceBusConnection__fullyQualifiedNamespace` = `my-namespace.servicebus.windows.net` |
| Event Hubs | `EventHubConnection` | `EventHubConnection__fullyQualifiedNamespace` = `my-namespace.servicebus.windows.net` |
| Cosmos DB | `CosmosDbConnection` | `CosmosDbConnection__accountEndpoint` = `https://my-account.documents.azure.com:443/` |
| Queue Storage | `StorageConnection` | `StorageConnection__queueServiceUri` = `https://mystorageacct.queue.core.windows.net` |
| Blob Storage | `StorageConnection` | `StorageConnection__blobServiceUri` = `https://mystorageacct.blob.core.windows.net` **and** `StorageConnection__queueServiceUri` (the blob trigger also uses an internal poison queue) |
| Functions host storage | `AzureWebJobsStorage` | `AzureWebJobsStorage__accountName` = `mystorageacct` |

To use a user-assigned identity for a connection, add `X__credential` = `managedidentity` and
`X__clientId` = `<client-id>` alongside.

The roles are granted to the **Function App's managed identity**, and the *host* needs storage
roles of its own on top of the per-trigger roles:

| Connection | Roles for the Function App's identity |
|---|---|
| Service Bus trigger | Azure Service Bus Data Receiver |
| Event Hubs trigger | Azure Event Hubs Data Receiver + **Storage Blob Data Owner** on the `AzureWebJobsStorage` account (the host stores Event Hubs checkpoints there) |
| Cosmos DB trigger | Cosmos DB Built-in Data Contributor (data-plane — see above; the trigger writes leases) |
| Queue Storage trigger | Storage Queue Data Contributor (it deletes handled messages and writes poison messages) |
| Blob Storage trigger | Storage Blob Data Owner (blob receipts) + Storage Queue Data Contributor (its internal poison queue) |
| `AzureWebJobsStorage__accountName` | Storage Blob Data Owner (plus Storage Queue Data Contributor / Storage Table Data Contributor if you use queue/Durable features) |

> **Consumption-plan caveat, honestly:** on the classic Linux Consumption (Y1) plan, the
> deployment content share (`WEBSITE_CONTENTAZUREFILECONNECTIONSTRING`) still requires a real
> connection string — identity-based `AzureWebJobsStorage` works fully on Elastic Premium,
> Dedicated, and Flex Consumption plans. The trigger connections
> (`ServiceBusConnection__fullyQualifiedNamespace` etc.) are identity-capable on every plan.
> Verify against current Azure documentation for your plan before removing the last key.

## Granting the roles

The well-known built-in role definition ids (stable, global constants):

| Role | Id |
|---|---|
| Azure Service Bus Data Receiver | `4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0` |
| Azure Service Bus Data Sender | `69a216fc-b8fb-44d8-bc22-1f3c2cd27a39` |
| Azure Event Hubs Data Receiver | `a638d3c7-ab3a-418d-83e6-5f17a39d4fde` |
| Azure Event Hubs Data Sender | `2b629674-e913-4c01-ae53-ef4638d8f975` |
| Storage Blob Data Contributor | `ba92f5b4-2d11-453d-a403-e96b0029c9fe` |
| Storage Blob Data Owner | `b7e6dc6d-f1e8-4753-8033-0f276bb0955b` |
| Storage Queue Data Contributor | `974c5e8b-45b9-4653-ba55-5f855dd0fb88` |

With the CLI (`--assignee` takes the identity's principal/object id):

```bash
az role assignment create \
  --assignee <principal-object-id> \
  --role "Azure Service Bus Data Receiver" \
  --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.ServiceBus/namespaces/my-namespace
```

In Bicep, alongside the resources (this is the standard shape; note `principalType` avoids
replication-delay failures on freshly created identities):

```bicep
resource serviceBusReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, functionApp.id, 'sb-data-receiver')
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions', '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0')
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}
```

Cosmos data-plane roles have their own Bicep resource type
(`Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments`) — same ids as the CLI table above.

The example template `examples/Azure/Benzene.Example.Azure/main.bicep` enables a system-assigned
identity on the Function App and outputs its `principalId`, ready for exactly these assignments.

## Local development

`DefaultAzureCredential` falls through to your `az login` (or Visual Studio) account, so the same
code runs locally — provided *your user* has the same data-plane roles on the dev resources.
Assign them once:

```bash
az role assignment create \
  --assignee $(az ad signed-in-user show --query id -o tsv) \
  --role "Azure Service Bus Data Receiver" \
  --scope <dev-namespace-resource-id>
```

The **emulators keep using connection strings** — the Service Bus/Event Hubs emulators and
Azurite don't do Entra ID. Benzene's own integration tests
(`test/Benzene.Integration.Test/`) stay connection-string-based for exactly that reason; managed
identity is for real Azure resources. This split (credential in config for real environments,
emulator connection string locally) is normal, not a smell.

## Troubleshooting

- **401 vs 403** — 401 means authentication failed (wrong tenant, no identity, expired local
  login); 403 means the identity authenticated fine but lacks a role. Start there.
- **Role assigned but still 403** — RBAC propagation is eventually consistent; a fresh
  assignment can take a few minutes. If it persists on Cosmos, you almost certainly granted an
  ARM role where a *data-plane* role was needed (see the Cosmos section).
- **Works in Azure, fails locally** (or vice versa) — log which credential the chain selected
  (`AZURE_LOG_LEVEL=verbose`, or use `DefaultAzureCredentialOptions.Diagnostics`); locally it
  should be Azure CLI, in Azure the managed identity. A common local failure is being logged
  into the wrong tenant: `az account show`.
- **Event Hubs worker starts, then dies checkpointing** — the namespace role is there but the
  storage role isn't. The checkpoint store needs Storage Blob Data Contributor *and* the
  container must already exist (`EventProcessorClient` doesn't create it).
- **Functions trigger never fires after switching settings** — the old `X` connection-string
  setting must be *removed*, not just supplemented; and check the host's own storage roles
  (`AzureWebJobsStorage__accountName` without Storage Blob Data Owner breaks the host quietly).
- **Slow first request locally** — the credential chain probes managed-identity endpoints that
  don't exist on your machine before falling through. Constrain it in dev if it bothers you
  (e.g. `new AzureCliCredential()` behind an `#if DEBUG` or configuration switch).

## See Also

- [Worker Service Setup](../getting-started-worker.md) — the three Azure workers this cookbook
  plugs credentials into
- [Azure Functions Setup](../azure-functions.md) — the trigger attributes whose `Connection`
  settings this cookbook replaces
- [Secrets & Multi-Cloud Configuration](secrets-configuration.md) — for the secrets you *can't*
  eliminate (third-party API keys), as opposed to the Azure-internal ones you now can
