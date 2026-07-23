# Multi-Tenancy

A multi-tenant B2B service handles requests for many customer organizations through one deployment,
and must attribute every request to a tenant and keep one tenant's data, cache, and side effects away
from another's. Benzene doesn't need a heavyweight tenancy framework for this — it already has the
exact seam: **per-message DI scope with a small scoped holder**, the same pattern
[`PresetTopicHolder`](../../src/Benzene.Core.MessageHandlers/CLAUDE.md) and
[`AuthenticationHolder`](auth-patterns.md) use. This cookbook shows the whole pattern end to end.

Benzene's job is to carry the *tenant context* through the pipeline. *Isolation* — a per-tenant cache
key prefix, connection string, or `WHERE tenant_id = …` — is the application using that context. That
split is deliberate: the storage policy is yours, the plumbing is the framework's.

## Problem statement

Every request belongs to a tenant. You need to (1) resolve which tenant, from a trustworthy source;
(2) make that available to handlers and outbound calls without threading it through every method; (3)
reject a request that should be tenant-scoped but isn't; and (4) use it to isolate data and cache.

## Step 1 — the scoped tenant holder

A plain POCO, registered **scoped** (one per message), read wherever the tenant is needed. It is
deliberately **not** a property on `TContext`: a context type describes a transport message's shape,
not optional cross-cutting state — see the "Context purity" convention in
`src/Benzene.Abstractions.Middleware/CLAUDE.md`.

```csharp
public class TenantHolder
{
    /// <summary>The current message's tenant, or null if none was resolved.</summary>
    public string? TenantId { get; set; }
}
```

## Step 2 — resolve the tenant into the holder

A resolver middleware runs early in the pipeline and sets `TenantHolder.TenantId`. Where the tenant
comes from is a strategy — pass it in as a delegate so the same middleware serves every source:

```csharp
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

public static partial class TenantExtensions
{
    /// <summary>
    /// Resolves the current message's tenant into a scoped <see cref="TenantHolder"/> using the
    /// supplied strategy. Register it early — after authentication (so a claim strategy can read the
    /// principal), before your handlers.
    /// </summary>
    public static IMiddlewarePipelineBuilder<TContext> UseTenant<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app,
        Func<TContext, IServiceResolver, string?> resolveTenant)
    {
        app.Register(x => x.TryAddScoped<TenantHolder>());

        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("ResolveTenant", async (context, next) =>
        {
            resolver.GetService<TenantHolder>().TenantId = resolveTenant(context, resolver);
            await next();
        }));
    }
}
```

### Strategy A — from the authenticated principal (recommended)

If the caller is authenticated (see [Authentication Patterns](auth-patterns.md)), the tenant is a
claim on the validated token — **tamper-proof**, because the caller can't forge a claim inside a
signed JWT. This is the strategy to prefer.

```csharp
using Benzene.Auth.Core;

app
    .UseOAuth2Bearer(oauth2Options)   // sets AuthenticationHolder.Principal
    .UseTenant<MyContext>((_, resolver) =>
        resolver.GetService<AuthenticationHolder>().Principal?.FindFirst("tid")?.Value);
```

### Strategy B — from a message header (internal / service-to-service)

Transport-agnostic, via `IMessageHeadersGetter<TContext>`. Only trust a client-supplied header for
isolation when the caller is trusted (an internal service, a gateway that already validated the
tenant) — see [Security notes](#security-notes).

```csharp
using Benzene.Abstractions.Messages.Mappers;

app.UseTenant<MyContext>((context, resolver) =>
{
    var headers = resolver.GetService<IMessageHeadersGetter<MyContext>>().GetHeaders(context);
    return headers != null && headers.TryGetValue("x-tenant-id", out var id) ? id : null;
});
```

### Strategy C — from the HTTP subdomain (HTTP only)

`acme.api.example.com` → `acme`. Read the host off the HTTP request adapter:

```csharp
using Benzene.Http;

app.UseTenant<AspNetContext>((context, resolver) =>
{
    var request = resolver.GetService<IHttpRequestAdapter<AspNetContext>>().Map(context).AsLowerCase();
    return request.Headers.TryGetValue("host", out var host) && host.Split('.') is { Length: > 2 } parts
        ? parts[0]
        : null;
});
```

## Step 3 — require a tenant where one is mandatory

Routes that must be tenant-scoped get a guard that short-circuits with `bad-request` when no tenant was
resolved — the same result-setter idiom the auth `Require*` middleware uses, so it returns a proper
status on every transport rather than throwing.

```csharp
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.Results;

public static partial class TenantExtensions
{
    public static IMiddlewarePipelineBuilder<TContext> RequireTenant<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app)
    {
        app.Register(x => x.TryAddScoped<TenantHolder>());

        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("RequireTenant", async (context, next) =>
        {
            if (string.IsNullOrEmpty(resolver.GetService<TenantHolder>().TenantId))
            {
                var messageGetter = resolver.GetService<IMessageGetter<TContext>>();
                var resultSetter = resolver.GetService<IMessageHandlerResultSetter<TContext>>();
                await resultSetter.SetResultAsync(context, new MessageHandlerResult(
                    messageGetter.GetTopic(context),
                    MessageHandlerDefinition.Empty(),
                    BenzeneResult.BadRequest("Missing tenant")));
                return;   // short-circuit: the handler never runs
            }

            await next();
        }));
    }
}
```

Compose them in the pipeline:

```csharp
app
    .UseOAuth2Bearer(oauth2Options)
    .UseTenant<MyContext>((_, r) => r.GetService<AuthenticationHolder>().Principal?.FindFirst("tid")?.Value)
    .RequireTenant<MyContext>()
    .UseMessageHandlers(x => x.AddHandlers());
```

## Step 4 — use the tenant to isolate

Inject `TenantHolder` wherever you need it — it's just a scoped service.

**Handler / data access** — filter every query by the tenant:

```csharp
public class GetOrdersHandler : IMessageHandler<GetOrders, OrderList>
{
    private readonly TenantHolder _tenant;
    private readonly IOrderRepository _orders;
    public GetOrdersHandler(TenantHolder tenant, IOrderRepository orders) { _tenant = tenant; _orders = orders; }

    public Task<IBenzeneResult<OrderList>> HandleAsync(GetOrders request)
        => _orders.ForTenant(_tenant.TenantId!).ListAsync();   // never a cross-tenant read
}
```

**Cache** — prefix keys so tenants can't read each other's cached values (see [Redis Caching](redis-caching.md)):

```csharp
var key = $"{_tenant.TenantId}:orders:{customerId}";
```

**Per-tenant connection string / database** — select the backing store from the tenant:

```csharp
var connectionString = _tenantConnectionMap.ForTenant(_tenant.TenantId!);
```

**Outbound propagation** — when this service calls another Benzene service, forward the tenant so the
downstream `UseTenant` (Strategy B) picks it up. Stamp it onto the outbound message's headers in your
client decorator, exactly as correlation/trace headers are forwarded (see
[Request Correlation](request-correlation.md)):

```csharp
outboundHeaders["x-tenant-id"] = _tenant.TenantId!;
```

## Testing

`TenantHolder` is a plain scoped service, so a handler test just constructs one with the tenant under
test. To test resolution + the guard, drive the middleware directly (as the auth tests do in
`test/Benzene.Core.Test/Auth/`): a resolver strategy returning `null` should make `RequireTenant`
short-circuit with `bad-request`; a present tenant should reach the handler.

## Security notes

- **Prefer the claim strategy (A).** A tenant claim inside a validated JWT can't be forged; a plain
  `x-tenant-id` header can be set to anything by whoever sends the request. Only trust a header for
  isolation when the sender is trusted (an internal service, or a gateway that already bound the
  tenant to the caller's identity).
- **Never derive isolation from a value the client fully controls without checking it** against the
  authenticated caller. Reading tenant `acme` from a header and then serving `acme`'s data to a caller
  who only owns `globex` is the classic multi-tenant data-leak — cross-check the resolved tenant
  against the principal when both are available.
- **Fail closed.** `RequireTenant` on every tenant-scoped route means a resolution bug becomes a
  `bad-request`, not an unscoped query that returns another tenant's rows.

## Do you need a package?

No — this is a page of code you own, over a seam Benzene already has, and every team's isolation
policy differs. If several services in your estate would copy the `TenantHolder` + `UseTenant` +
`RequireTenant` trio verbatim, factor it into a small shared internal library; there's nothing
Benzene-specific left to add once the holder and the two middleware exist.

## Further reading

- [Authentication Patterns](auth-patterns.md) — the `AuthenticationHolder`/claim source for Strategy A.
- `src/Benzene.Core.MessageHandlers/CLAUDE.md` — `PresetTopicHolder`, the scoped-holder pattern this mirrors.
- `src/Benzene.Abstractions.Middleware/CLAUDE.md` — the "Context purity" convention (why tenant is a holder, not a `TContext` property).
