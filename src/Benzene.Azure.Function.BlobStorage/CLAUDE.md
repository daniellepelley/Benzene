# Benzene.Azure.Function.BlobStorage

## What this package does
Inbound Azure Blob Storage adapter for the Azure Functions `BlobTrigger` binding (isolated
worker): delivers an uploaded/updated blob (name + content) to a Benzene middleware pipeline.
The loose Azure counterpart of `Benzene.Aws.Lambda.S3` — but where S3 delivers *event
notifications* (bucket/key, no content) that route as messages, the blob trigger delivers the
**content itself**, which shapes the whole design (below).

## Zero dependencies — deliberately
References only `Benzene.Azure.Function.Core` + `Benzene.Core.MessageHandlers` — no storage SDK,
no Functions extension package. The consumer's Function App project references
`Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs` itself for the `[BlobTrigger]`
attribute, binds the content as `byte[]` (or `string`) and the `{name}` expression, and calls
`HandleBlob(name, content)`. Do not add SDK packages here without asking first (repo NuGet
policy).

## Not message-routed — deliberately
There is no `UseMessageHandlers()`/`UseBenzeneMessage` path here: a blob is a *file*, not a
message envelope, and one blob-trigger function watches one container path — there is no routing
dimension. Like `Benzene.Azure.Function.CosmosDb` (the other non-routed Azure adapter), the
pipeline is consumed directly, with `UseBlob(...)` as the terminal sugar (the blob counterpart of
the streaming engine's `UseStream(...)`), composing with correlation/metrics/exception middleware
on the same builder:

```csharp
app.UseBlobStorage(blob => blob
    .UseBlob(async delivered =>
    {
        // delivered.Name, delivered.Content, delivered.GetContentAsString()
    }));
```

## Declared triggers (source-generated)
Instead of hand-writing the `[Function]`/`[…Trigger]` class, declare the trigger and let
Benzene's source generator (shipped in `Benzene.Azure.Function.Core`) emit it:
`[assembly: BenzeneBlobTrigger(Name = "ingest", Path = "incoming/{name}")]`.
`BenzeneBlobTriggerAttribute` (assembly-scoped, `AllowMultiple`) lives in this package; you own every
binding value. Still reference this transport's `Microsoft.Azure.Functions.Worker.Extensions.*`
package directly, and note `FunctionsEnableWorkerIndexing=false` (auto via Core's
buildTransitive). The hand-written form still works. See `docs/azure-functions.md`.

## Key types
- `BlobTriggerEvent` — Benzene's own dependency-free model of a delivery: `Name` (the trigger's
  `{name}` binding value), `Content` (`byte[]`), `GetContentAsString()` UTF-8 helper.
- `BlobStorageContext` — wraps one `BlobTriggerEvent`. No `IHasMessageResult` — nothing routes,
  so nothing records a routed-handler result.
- `BlobStorageApplication` — `EntryPointMiddlewareApplication<BlobTriggerEvent>`,
  transport-tagged `"blob-storage"`, one DI scope per invocation.
- `UseBlobStorage(action)` (both `IAzureFunctionAppBuilder` and platform-neutral
  `IBenzeneApplicationBuilder`, no-op off-Azure), `UseBlob(...)` (context and event overloads),
  `HandleBlob(name, byte[])` / `HandleBlob(name, string)` (UTF-8).

## Failure handling
A pipeline exception propagates to the Functions host: the trigger retries up to 5 times and then
writes a poison entry to the `webjobs-blobtrigger-poison` queue. Also worth knowing (host
behavior, not Benzene's): the classic blob trigger is polling-based via blob receipts — delivery
can lag on large containers, and the host recommends Event Grid-based triggers for latency-
sensitive work. Neither concern changes anything in this package.

## No TestHelpers package
Deliberate: the dispatch surface is `HandleBlob(name, content)` with primitive arguments — there
is nothing to build.

## No egress package — deliberately (release plan §5.2)
There is no `Benzene.Clients.Azure.BlobStorage`. Blob Storage is a **store**, not a transport —
writing a blob is storage access, the same category as writing to a database. Benzene doesn't get
involved in database/storage access (design philosophy principle 2 — see the
[Capability Matrix](../../docs/capability-matrix.md)); use `BlobClient`/`BlobContainerClient`
directly in your own handler code.

## Tests
- `test/Benzene.Core.Test/Azure/BlobStoragePipelineTest.cs` — delivery of name+content, UTF-8
  string overload round-trip, exception propagation, platform-neutral no-op overload.
