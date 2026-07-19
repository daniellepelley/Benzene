# Benzene.Configuration.Core

## What this package does
The neutral **secrets & multi-cloud configuration** abstraction (gap-analysis A.5). Decouples an
application from *where* its secrets and config actually live so the same code ports across clouds:
the app depends on `ISecretStore`; a provider adapter implements it. Ships the mechanism-agnostic
core BCL-only — no cloud SDK dependency. Cloud adapters (Azure Key Vault, AWS Secrets Manager, SSM
Parameter Store, Azure App Configuration) are a one-method implementation each; the cookbook shows
copy-paste versions rather than this package taking on those SDK dependencies. See the
[Capability Matrix](../../docs/capability-matrix.md)'s *Configuration & secrets* row for why the
cloud adapters ship as cookbook snippets rather than maintained packages (a post-1.0 candidate).

## Key types
- `ISecretStore` — the whole seam: `Task<string?> GetSecretAsync(string name, CancellationToken)`.
  Returns `null` when this store doesn't have the name (so a composite can fall through). Covers both
  secrets (a DB password) and plain config (an endpoint) — both are "a named value from a source".
- **Providers (BCL-only, all tested):**
  - `InMemorySecretStore` — dictionary-backed; tests, local defaults, composite's bottom layer.
  - `EnvironmentVariableSecretStore(prefix)` — maps a logical name to an env-var key by upper-casing
    and replacing `: . -` and spaces with `_` (`Db:Password` → `DB_PASSWORD`), with an optional
    prefix. Key mapping is `EnvironmentVariableSecretStore.ToEnvironmentVariableKey`.
  - `FileSecretStore(directory)` — one file per secret (Docker/K8s `/run/secrets/<name>` mount
    convention); trims a trailing newline, preserves other whitespace.
  - `CompositeSecretStore(params stores)` — ordered, first non-null wins (env overrides cloud, etc.).
  - `CachingSecretStore(inner, ttl, now)` — the **optional-reload** seam: caches (incl. misses) for a
    TTL so a remote store isn't hit per read; `Invalidate(name)`/`InvalidateAll()` force re-fetch
    after a rotation. Injectable clock for tests.
- `SecretResolver(store)` — ergonomic typed reads for building config at startup: `RequireAsync`
  (throws `MissingSecretException` if absent/blank — **fail fast**), `GetAsync(name, default)`, and
  `RequireIntAsync`/`RequireBoolAsync`/`RequireUriAsync` (throw `FormatException` on a malformed
  present value).
- `SecretValidation.EnsureRequiredAsync(store, names...)` — startup completeness check; throws
  `MissingSecretException` listing **all** missing names at once, so a misconfigured deploy fails
  before serving traffic, not one redeploy at a time.
- `MissingSecretException` — carries `MissingNames`.
- `Extensions.AddSecretStore(store)` / `AddSecretStores(params stores)` — register the `ISecretStore`
  (+ a `SecretResolver`) as singletons on `IBenzeneServiceContainer`; the multi-store overload wraps
  them in a `CompositeSecretStore`.

## Conventions / design boundary
- **BCL-only.** No cloud SDK dependency is taken here — that's the whole portability point (§2.7). A
  provider adapter lives in the consuming app (or a future satellite package) and implements the
  single `GetSecretAsync`. Same "core owns the mechanism, adapters at the edge" split as
  `Benzene.HealthChecks.Core`/`Benzene.Auth.Core`.
- **Secrets fetch is async and done at startup**, before wiring the pipeline: resolve + validate,
  then register the concrete values/typed options as singletons. No sync-over-async, no magic
  attribute binding — the app assigns its own typed options object from a `SecretResolver`, shown in
  the cookbook.
- Never log a resolved secret value or put it on a `TContext`.

## Docs
- Cookbook `docs/cookbooks/secrets-configuration.md` — the startup pattern, composition, reload, and
  copy-paste Key Vault / AWS Secrets Manager / SSM adapters.

## Tests
- `test/Benzene.Core.Test/Configuration/SecretStoresTest.cs` — in-memory, env-var mapping + read,
  file read/trim, composite first-non-null, caching TTL/invalidate (injected clock, counting inner).
- `test/Benzene.Core.Test/Configuration/SecretResolverAndValidationTest.cs` — require/get/typed
  parse, and `EnsureRequiredAsync` passing vs. throwing with the full missing list.
