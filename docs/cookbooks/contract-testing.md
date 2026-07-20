# Contract Testing (catching breaking changes before they reach consumers)

Benzene services talk to each other by topic + payload, and consumers often use a CodeGen-generated
strongly-typed client. That makes it possible to catch a provider changing its contract in a way
that breaks its consumers — two complementary mechanisms, one at **runtime** and one at **build/CI
time**.

## Problem statement

A service evolves its message contract (a handler's request/response shape, or the set of topics it
answers). Some changes are safe (adding an optional response field); some break consumers (removing
a response field, adding a required request field). You want to know **before** the change ships,
not from a production incident.

## Mechanism 1 — runtime contract-drift check

Every generated client bakes in a hash of the contract it was built against
(`{Service}ServiceClient.HashCode`) and can call the provider's health check to compare it against
the provider's *current* contract hash. The provider publishes that hash from a schema health check.

**Provider** — register the schema health check (`Benzene.HealthChecks.Schema`):

```csharp
using Benzene.HealthChecks.Schema;

app.UseHealthCheck("get", "healthcheck", health => health
    .AddSchemaHealthCheck()          // publishes the live contract hash as a "schema" health check
    .AddHealthCheck("live", _ => true));
```

**Consumer** — the generated client's `HealthCheckAsync()` fetches the provider's health check and
compares hashes (`Benzene.Clients.HealthChecks.ClientHealthCheckProcessor`); a mismatch means the
provider's contract has drifted from what the client was generated against. Both ends hash with the
same `CodeGenHelpers.GenerateHash`, so the hashes are directly comparable.

This is reactive — it tells you drift has already happened. For a pre-merge stop, use mechanism 2.

## Mechanism 2 — CI compatibility gate

`SchemaCompatibility.EnsureBackwardCompatible(...)` compares the current contract against a committed
baseline and **throws on breaking changes** (while allowing additive ones), so a plain test fails
the build.

### 1. Commit a baseline spec

Generate the service's contract and commit it as `spec.baseline.json` (the OpenAPI/event-service
document — the same one CodeGen and the schema health check hash). Regenerate it deliberately
whenever you *intend* a breaking change.

### 2. Add a gate test

```csharp
using Benzene.Schema.OpenApi.Compatibility;
using Benzene.Schema.OpenApi.EventService;

[Fact]
public void Contract_IsBackwardCompatibleWithBaseline()
{
    var baselineJson = File.ReadAllText("spec.baseline.json");

    // The current contract, built from this service's handler definitions. Get them however your
    // app exposes them - e.g. resolve IMessageHandlerDefinitionLookUp and call GetAllHandlers(),
    // the same source the provider schema health check uses.
    var current = lookUp.GetAllHandlers().ToEventServiceDocument();

    // Throws SchemaCompatibilityException (failing the test) on any breaking change.
    SchemaCompatibility.EnsureBackwardCompatible(baselineJson, current);
}
```

`EnsureBackwardCompatible` returns the report (additive changes + warnings) when compatible, and
throws with a message listing every breaking change when not. Overloads accept two
`EventServiceDocument`s, `(baselineJson, current)`, or `(baselineJson, currentJson)`.

### 3. What counts as breaking

The default rules are direction-aware (see `SchemaCompatibilityRules.DefaultFor`):

| Change | Request | Response |
|---|---|---|
| Topic added | compatible | — |
| Topic removed | **breaking** | **breaking** |
| Optional property added | compatible | compatible |
| Required property added | **breaking** | compatible |
| Property removed | warning | **breaking** |
| Type changed | **breaking** | **breaking** |

Pass `SchemaCompatibilityRules.Strict()` to treat every non-compatible change as breaking, or
`.Set(kind, direction, compatibility)` to override individual rules.

## Further reading

- [Client SDKs](../client-sdks.md) — the generated typed clients that bake in the contract hash.
- [Health Checks](../health-checks.md) — registering the provider health check.
