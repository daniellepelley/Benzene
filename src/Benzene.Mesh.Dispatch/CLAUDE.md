# Benzene.Mesh.Dispatch

## What this package does
The **opt-in, production-gated live dispatch** capability for the mesh — F3b-revised **case (1)**, the
direct-to-consumer path. It serves a `mesh:dispatch` handler that invokes **ONE** registered service's
**real** handler with a caller-supplied payload and returns the response, so the mesh UI's payload
composer can *send* (not just copy) a test message to a service.

This is the write-side counterpart of `Benzene.Mesh.Aggregator`'s read-only `IMeshServiceSource`
(which only fetches spec/health). It reuses the **same access the aggregator already has** to reach a
service — HTTP POST, or an AWS Lambda `Invoke` — changing the *payload*, not the *permission*, and is
bounded to a single declared service (never a shared queue). That is why it clears §10.7's bar the
rejected queue-injection version didn't; see `work/mesh-ui-product-vision.md` F3b-revised.

## Why it's gated (this is the load-bearing part)
Dispatch fires a **real handler with real side-effects** (DB writes, downstream calls, the handler's
own publishes). So it is off by two independent gates:
1. **Opt-in registration** — nothing is exposed unless `UseMeshDispatch()` is called. The handler
   carries **no `[Message]` attribute**, so a plain `.UseMessageHandlers()` assembly-scan does **not**
   auto-discover it (unlike `mesh:report`); only the explicit `UseMeshDispatch()` registration routes it.
2. **Runtime environment gate** — `MeshDispatchGate` refuses dispatch in a **Production** environment
   unless `MeshDispatchOptions.AllowInProduction` is set. An **unset** `ASPNETCORE_ENVIRONMENT` /
   `DOTNET_ENVIRONMENT` counts as Production (the safe default), so dispatch is off unless the host is
   explicitly a non-production environment (or the override is set). A blocked dispatch returns
   `Forbidden` with the reason — it never silently runs.

## Key types
- `MeshDispatchOptions { bool AllowInProduction }` — the second opt-in (default false).
- `IMeshDispatchEnvironment` / `EnvironmentVariableMeshDispatchEnvironment` — "is this Production?",
  defaulting to the env-var reading above (overridable in DI, e.g. for tests).
- `MeshDispatchGate` — `IsAllowed` (= non-Production **or** AllowInProduction) + `BlockedReason`.
- `IMeshServiceDispatcher { string Key; DispatchAsync(entry, envelope, ct) }` — reach ONE service over
  one transport, keyed by `MeshServiceRegistryEntry.Source` (the same keying `IMeshServiceSource` uses).
  - `HttpMeshServiceDispatcher` (`Key = "Http"`) — POSTs the `{ topic, headers, body }` envelope to the
    service's invoke URL (`SourceOptions["invokeUrl"]`, else `<specUrl origin>/benzene-message`).
  - `AwsLambdaMeshServiceDispatcher` (`Key = "AwsLambdaInvoke"`) lives in **`Benzene.Mesh.Aws.Lambda`**
    (`AddMeshLambdaDispatcher()`), reusing that package's `IAwsLambdaClient` / `lambda:InvokeFunction`
    grant — the AWS SDK stays out of this core package.
- `MeshDispatchRequest { Service, Topic, Headers?, Body? }` — the `mesh:dispatch` body.
- `MeshDispatchMessageHandler` — gate → resolve the target from the injected `MeshServiceRegistry` by
  name → pick the dispatcher by `entry.Source` → dispatch → return the service's `{ statusCode, headers,
  body }`. Distinct statuses per failure: `Forbidden` (gated off), `BadRequest` (no service/topic),
  `NotFound` (unknown service), `NotImplemented` (no dispatcher for that source).
- `Extensions.UseMeshDispatch<TContext>(options?)` — opt-in registration (registers the handler on
  `mesh:dispatch`, the options/gate, and the HTTP dispatcher). Requires a `MeshServiceRegistry` in DI
  (the dispatchable set) and, for AWS-Lambda services, `AddMeshLambdaDispatcher()`.

## When to use
Only when you deliberately want the mesh to *send* live test messages to services (a dev/staging
convenience). Wire it (gated) into a mesh host — see `deploy/Mesh/Benzene.Mesh.Host` (`EnableDispatch`
/ `DispatchAllowInProduction` config, off by default). For copy-only payloads, or queue/stream
transports (which stay compose+copy only), you don't need this package at all — that's `UseTestPayloads()`
+ the mesh UI's F3a composer.

## Dependencies
- **Benzene.Mesh.Contracts** — `MeshServiceRegistry`/`MeshServiceRegistryEntry`/`MeshServiceSource`.
- **Benzene.Abstractions.MessageHandlers** / **Benzene.Core.MessageHandlers** — the handler + its
  registration; `RawStringMessage`/`BenzeneResult` transitively. No AWS dependency (the AWS dispatcher
  is in `Benzene.Mesh.Aws.Lambda`).

## Tests
`test/Benzene.Mesh.Test/MeshDispatchTest.cs` — the gate truth table, the handler's gate/not-found/
bad-request/no-dispatcher/happy paths (with a recording fake dispatcher, asserting a blocked dispatch
never reaches the dispatcher), and the AWS dispatcher's invoke mapping (mocked `IAwsLambdaClient`).

## Follow-ups (not in this package yet)
- The mesh UI **send leg**: wiring the F3a composer's existing envelope + a "Send" button to POST
  `mesh:dispatch`, feature-detected like the annotations/fleet endpoints and compose-toggle gated.
- Discovery-driven meshes (e.g. `examples/AwsMesh`, whose registry is replaced at runtime and persisted
  to S3) need the live registry surfaced to the handler; the static-config `Benzene.Mesh.Host` is the
  wired first target.
