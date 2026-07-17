# Secrets & Multi-Cloud Configuration

Enterprise services need their secrets (database passwords, API keys, signing keys) out of plaintext
config and image layers, validated at startup so a missing credential fails fast, and — for anyone
running on more than one cloud, or planning to — behind an abstraction that doesn't hard-code a
single provider. `Benzene.Configuration.Core` gives you that: a one-method `ISecretStore` seam, a set
of BCL-only providers, composition, caching/reload, and typed fail-fast resolution. Cloud provider
adapters are a few lines each (below) — the core package takes on no cloud-SDK dependency.

## Problem statement

Your service reads a DB password and an API key. You want them sourced from a real secret store in
production (Key Vault, AWS Secrets Manager), overridable by environment variables locally, validated
at startup (not on the first request that needs them), and fetched through code that doesn't change
when you move clouds.

## The abstraction

```csharp
public interface ISecretStore
{
    Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);
}
```

Everything else — env/file/in-memory providers, composition, caching, validation, typed reads — is
layered on top of that one method, so a provider adapter stays trivial.

## Step 1 — compose your stores

Layer an environment-variable override in front of your real store, first non-null wins:

```csharp
using Benzene.Configuration.Core;

ISecretStore secrets = new CompositeSecretStore(
    new EnvironmentVariableSecretStore(prefix: "MyApp:"), // local/dev override, checked first
    new CachingSecretStore(                               // cache the remote store (5 min default)
        new KeyVaultSecretStore("https://myapp-kv.vault.azure.net/"))); // see "Cloud adapters" below
```

- `EnvironmentVariableSecretStore` maps `Db:Password` → `MYAPP_DB_PASSWORD` (upper-cased, `: . -` and
  spaces become `_`, plus the prefix).
- `CachingSecretStore` avoids hitting the remote store on every read; a value refreshes when its TTL
  lapses, and `Invalidate(name)`/`InvalidateAll()` force an immediate re-fetch after a rotation.
- `FileSecretStore("/run/secrets")` is the Docker/Kubernetes secret-mount option (one file per
  secret).

## Step 2 — validate at startup (fail fast)

Verify everything the service needs resolves *before* it starts serving, so a misconfiguration is one
immediate, complete error — not a first-request failure deep in a handler:

```csharp
await SecretValidation.EnsureRequiredAsync(secrets, "Db:Password", "Api:Key");
// throws MissingSecretException listing ALL missing names at once
```

## Step 3 — build typed config and register it

Secrets fetch is async and belongs in startup, before the pipeline is wired. Resolve into your own
typed options object and register it — no reflection, no sync-over-async:

```csharp
var resolver = new SecretResolver(secrets);

var options = new MyServiceOptions
{
    DbPassword  = await resolver.RequireAsync("Db:Password"),      // throws if absent/blank
    ApiKey      = await resolver.RequireAsync("Api:Key"),
    MaxRetries  = await resolver.GetAsync("MaxRetries") is { } r ? int.Parse(r) : 3,
    Endpoint    = await resolver.RequireUriAsync("Endpoint"),       // typed, parses or throws
};

services.AddSingleton(options);
```

Or register the store itself so handlers can resolve secrets on demand:

```csharp
services.AddSecretStore(secrets);   // registers ISecretStore + a SecretResolver as singletons
// (or services.AddSecretStores(envStore, cloudStore) to compose + register in one call)
```

## Cloud adapters (copy-paste)

Each provider is a one-method `ISecretStore`. Drop the one you need into your app and add its SDK
package — the core package stays dependency-free so you only pull in the cloud you actually use.

### Azure Key Vault — `Azure.Security.KeyVault.Secrets`

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Benzene.Configuration.Core;

public class KeyVaultSecretStore : ISecretStore
{
    private readonly SecretClient _client;

    public KeyVaultSecretStore(string vaultUri)
        => _client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());

    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            // Key Vault names allow [0-9a-zA-Z-]; map ':'/'.' from logical names to '-'.
            var response = await _client.GetSecretAsync(name.Replace(':', '-').Replace('.', '-'), cancellationToken: cancellationToken);
            return response.Value.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null; // not found -> let a CompositeSecretStore fall through
        }
    }
}
```

Wrap it in `CachingSecretStore` (above) so you're not calling Key Vault on every read.

### AWS Secrets Manager — `AWSSDK.SecretsManager`

```csharp
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Benzene.Configuration.Core;

public class SecretsManagerSecretStore : ISecretStore
{
    private readonly IAmazonSecretsManager _client;
    public SecretsManagerSecretStore(IAmazonSecretsManager client) => _client = client;

    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = name }, cancellationToken);
            return response.SecretString;
        }
        catch (ResourceNotFoundException)
        {
            return null;
        }
    }
}
```

### AWS SSM Parameter Store — `AWSSDK.SimpleSystemsManagement`

```csharp
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Benzene.Configuration.Core;

public class SsmParameterStore : ISecretStore
{
    private readonly IAmazonSimpleSystemsManagement _client;
    private readonly string _pathPrefix;   // e.g. "/myapp/"

    public SsmParameterStore(IAmazonSimpleSystemsManagement client, string pathPrefix = "/")
    {
        _client = client;
        _pathPrefix = pathPrefix;
    }

    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetParameterAsync(
                new GetParameterRequest { Name = _pathPrefix + name.Replace(':', '/'), WithDecryption = true },
                cancellationToken);
            return response.Parameter.Value;
        }
        catch (ParameterNotFoundException)
        {
            return null;
        }
    }
}
```

Azure App Configuration (for non-secret config) follows the same shape over its own client.

## Testing

Everything in the core is BCL-only and unit-testable without a cloud: `InMemorySecretStore` for
seeded values, `CachingSecretStore`'s injectable clock for TTL behaviour, and a plain fake
`ISecretStore` to prove your composition/validation wiring. See
`test/Benzene.Core.Test/Configuration/` for worked examples.

## Security notes

- **Never log a resolved secret**, and never put one on a `TContext` — resolve it in startup and keep
  it in your typed options / DI.
- **Prefer workload identity** (`DefaultAzureCredential`, an EC2/EKS instance role) over a static
  bootstrap credential for reaching the secret store — that removes the "secret to reach the secrets"
  problem.
- **`FileSecretStore` and mounted secrets** keep credentials out of environment variables (which leak
  into child processes and crash dumps more readily) and out of image layers.

## Further reading

- `src/Benzene.Configuration.Core/CLAUDE.md` — the type-by-type reference.
- `work/enterprise-adoption-gap-analysis.md` A.5 — the design context and the abstraction-vs-adapters
  scope decision.
