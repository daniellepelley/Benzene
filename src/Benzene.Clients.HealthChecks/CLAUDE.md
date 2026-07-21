# Benzene.Clients.HealthChecks

## What this package does
The **consumer side** of Benzene's contract-drift check. Given a health-check response already fetched
from a provider service, it compares the provider's live message-contract hash against the hash the
consumer's generated client was built against, and annotates the result with the verdict. It does
**not** call any HTTP endpoint or `/health` route itself and has no timeout logic — fetching the
provider's health response is the caller's job; this package only processes the response. The provider
side that publishes the hash is `Benzene.HealthChecks.Schema`.

## Key types/interfaces
- `ClientHealthCheckProcessor.Process(IHealthCheckResponse<HealthCheckResult>, string hashCode)` -
  static. Finds the provider's `"schema"`-typed health check (via
  `SchemaHealthCheckConstants.Type`), reads its published hash out of `Data`, compares it with
  `hashCode` (the hash the client was generated against), and writes a `ClientHashMatch` into the
  schema check's `Data`. If there is no schema check to compare against, it passes the response through
  unchanged. The published hash is normalized with `ToString()`, so it works whether it arrives as a
  plain string, a System.Text.Json `JsonElement`, or a Newtonsoft `JToken`.
- `ClientHashMatch` - the verdict: `ServiceHashCode`, `ClientHashCode`, `IsMatch`.
- `IHasHealthCheck` - `HashCode` + `Task<IBenzeneResult<HealthCheckResponse>> HealthCheckAsync()`, the
  contract a generated client exposes so its baked-in hash and a health call are available together.
- `ClientHealthCheck` - an `IHealthCheck` adapter over one generated client (`IHasHealthCheck` + a
  service name). It calls `HealthCheckAsync()` (whose response is already drift-annotated by
  `ClientHealthCheckProcessor`) and folds that aggregated response into one check result: reachable +
  matching contract -> `Ok`, reachable + drift -> `Warning` (degraded-not-fatal, does not flip
  `IsHealthy`), unreachable/throws -> `Failed`; attaches a `HealthCheckDependency("Service", name)`.
  It tracks the *contract* relationship, not the provider's transient internal health (that's the
  provider's own readiness concern).
- `ContractHealthCheckExtensions` - `AddContractCheck<TClient>(serviceName)` (resolves the generated
  client from DI) / `AddContractCheck(serviceName, client)` (explicit instance) register a
  `ClientHealthCheck` on a health-check builder.

## Wiring: the `contracts` topic, NEVER a probe
Register contract checks on the dedicated **`contracts`** diagnostic topic via
`UseContractsCheck(x => x.AddContractCheck<IOrderServiceClient>("OrderService"))`
(`UseContractsCheck` lives in `Benzene.HealthChecks`; `Constants.DefaultContractsTopic = "contracts"`).
This topic is deliberately kept off the Kubernetes liveness/readiness probes: a contract check calls a
downstream service and reports drift, so putting it in a probe would let one struggling dependency (or
a compatible-but-changed contract) restart or de-route otherwise-healthy pods. Feed it to monitoring /
the mesh instead. See `docs/kubernetes-health-checks.md` and `work/client-health-checks-design.md`.

## When to use this package
- On a consumer service that uses a CodeGen-generated typed client, to turn a fetched provider health
  response into a contract-match/drift verdict.

## Dependencies on other Benzene packages
- **Benzene.Clients** - client abstractions
- **Benzene.HealthChecks** - `HealthCheckResult`/`HealthCheckResponse`, `SchemaHealthCheckConstants`
- **Benzene.Results** - `IBenzeneResult`

## Important conventions
- This is a pure comparison/annotation step - no network I/O, no endpoint URL, no timeout here.
- The `"schema"` type + `Data`-key strings are shared with the provider via
  `SchemaHealthCheckConstants` so the two halves can't drift on a literal.
