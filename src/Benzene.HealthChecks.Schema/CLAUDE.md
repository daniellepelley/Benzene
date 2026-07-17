# Benzene.HealthChecks.Schema

## What this package does
The **provider side** of Benzene's contract-drift check: `SchemaHealthCheck` hashes a service's
current message contract (every registered handler's topic + request/response schema) and publishes
the hash as a `"schema"`-typed health check, so consumers can detect when the contract has drifted
from what their generated client was built against. This revives the (long-commented-out) legacy
`MessageSchemaHealthCheck` and completes the loop the current repo only had the consumer half of.

## The contract-drift loop
```
provider: SchemaHealthCheck  ──"schema" health check {hashCode}──▶  consumer: ClientHealthCheckProcessor
            (this package)                                            (Benzene.Clients.HealthChecks)
                 ▲                                                              ▲
   CodeGenHelpers.GenerateHash(live handlers)          {Service}ServiceClient.HashCode (baked at codegen)
                 └──────────────── same hash function ─────────────────────────┘
```
- **`SchemaHealthCheck`** resolves `IMessageHandlerDefinitionLookUp`, calls
  `CodeGenHelpers.GenerateHash(GetAllHandlers())` (`Benzene.CodeGen.Core`), and returns a health
  result of `Type = "schema"` with the hash under `Data["hashCode"]`.
- **Crucially**, it uses the *same* `CodeGenHelpers.GenerateHash` that `Benzene.CodeGen.Client` bakes
  into a generated `{Service}ServiceClient.HashCode`. So the live provider hash and the consumer's
  baked-in hash are directly comparable - equal means no drift, different means the contract changed.
- The wire contract (the `Type` and `Data` key strings) lives in
  `Benzene.HealthChecks.Core.SchemaHealthCheckConstants`, referenced by both this package and the
  consumer-side processor so they can't drift on a literal.

## Key types
- `SchemaHealthCheck : IHealthCheck` - the provider health check.
- `SchemaHealthCheckExtensions.AddSchemaHealthCheck(this IHealthCheckBuilder)` - registration;
  resolves the handler lookup from DI when the check runs.

## When to use
- On any Benzene service whose consumers use CodeGen-generated typed clients, so a consumer's
  health check turns to a mismatch verdict the moment the provider's contract drifts.

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - `IMessageHandlerDefinitionLookUp`, DI `IServiceResolver`.
- **Benzene.CodeGen.Core** - `CodeGenHelpers.GenerateHash` (the canonical contract hash; lightweight,
  only pulls in `Benzene.Schema.OpenApi`, no Roslyn).
- **Benzene.HealthChecks.Core** - `IHealthCheck`, `HealthCheckResult`, `SchemaHealthCheckConstants`.

## Conventions / notes
- The hash is published as a **plain string** under `Data["hashCode"]` (not a nested object) so it
  survives any JSON round-trip; the consumer reads it via `ToString()` rather than `dynamic`, so it's
  robust whether the value arrives as a string, a System.Text.Json `JsonElement`, or a Newtonsoft
  `JToken`. (The old consumer processor used `dynamic Data["data"].hashCode`, which only worked under
  Newtonsoft - hardened as part of this change.)
- Runtime hashing goes through the same `EventServiceDocument` normalization CodeGen uses (strips
  generated examples + the endpoint advert), so a service upgrade doesn't trip a false mismatch - see
  `CodeGenHelpers.GenerateHash`'s doc comment.
- Test coverage: `test/Benzene.Core.Test/HealthChecks/SchemaHealthCheckTest.cs` (canonical-hash +
  end-to-end match/drift) and `test/Benzene.Core.Test/Clients/ClientHealthCheckProcessorTest.cs`
  (processor robustness incl. the JsonElement wire-round-trip case).
- **Not yet built (A.2b):** a CI-time breaking-vs-additive gate on top of the existing
  `Benzene.Schema.OpenApi.Compatibility.SchemaCompatibility` comparer - this package is the runtime
  half only. See `work/enterprise-adoption-gap-analysis.md` A.2.
