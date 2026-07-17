# Benzene.Auth.Core

## What this package does
The pure-contracts half of `work/auth-middleware-design.md`'s authentication feature: the scoped
DI seam a concrete authentication middleware (`Benzene.Auth.Basic`, `Benzene.Auth.OAuth2`) uses to
hand a validated caller identity to later pipeline steps, plus the shared
`Unauthorized`/`Forbidden` result-building helper both packages short-circuit with. No third-party
dependency beyond the BCL - following the `Benzene.HealthChecks.Core` + `Benzene.HealthChecks`
split precedent, this package holds only what's mechanism-agnostic; everything specific to Basic
auth or OAuth2/JWT lives in its own concrete package.

## Key types
- `AuthenticationHolder` - the Context Purity seam (see
  `Benzene.Abstractions.Middleware/CLAUDE.md`'s "Context purity" section, and
  `Benzene.Core.MessageHandlers`' `PresetTopicHolder` for the worked example this follows). Plain
  POCO, `ClaimsPrincipal? Principal { get; set; }`, no interface. Registered scoped by each
  concrete auth middleware's own `Use*` extension (`services.TryAddScoped<AuthenticationHolder>()`)
  - **not centrally** - so a pipeline that never adds an authentication middleware never allocates
  a holder anyone would look at. `Principal` stays `null` until an authentication middleware sets
  it; a `null` principal downstream means "no authentication middleware ran, or the one that ran
  failed" - callers can't distinguish those two cases from the holder alone, which is intentional
  (see `RequireScope` in `Benzene.Auth.OAuth2`, which treats both identically: `Unauthorized`).
- `AuthResults` - `UnauthorizedAsync<TContext>(resolver, context, detail)` /
  `ForbiddenAsync<TContext>(resolver, context, detail)`. Mirrors the exact idiom
  `Benzene.HealthChecks`' `UseHealthCheckMiddleware` and `Benzene.Http`'s `CorsMiddleware` already
  use for "middleware short-circuits with a status + detail message": resolves
  `IMessageHandlerResultSetter<TContext>` and applies a `BenzeneResult.Unauthorized(detail)`/
  `BenzeneResult.Forbidden(detail)` (see `Benzene.Results`) wrapped in a `MessageHandlerResult`
  against the message's real topic (resolved via `IMessageGetter<TContext>`, `MessageHandlerDefinition.Empty()`
  as the definition - no specific handler ran). `detail` ends up as the wire error payload's
  `Detail` field (`docs/specification/wire-contracts.md` §1.3) - keep it caller-safe; see
  `Benzene.Auth.OAuth2/CLAUDE.md` for why JWT validation failures never pass their real exception
  message through this helper.

## Authorization layer (A.4 — RBAC/policies over the principal)
The mechanism-agnostic authorization primitives the auth design (`work/auth-middleware-design.md`
§4) deliberately left as "an app concern layered on top of the `ClaimsPrincipal`". They read the
scoped `AuthenticationHolder.Principal` an authentication middleware set, and short-circuit via
`AuthResults` — `Unauthorized` when there's no caller, `Forbidden` when the caller lacks
permission. All live here (not a mechanism package) because they read a plain `ClaimsPrincipal` and
so aren't tied to JWTs the way OAuth2 scopes are; and unconstrained on `TContext` (no `IHttpContext`
bound) because authorization only reads the principal, so it composes on any transport whose
pipeline sets one. All are in `AuthorizationExtensions`:
- `RequireRole<TContext>(params string[] anyOfRoles)` - any-one-of role check. Roles via
  `RoleClaims` (internal): `ClaimsPrincipal.IsInRole` **plus** the common role claim types
  (`ClaimTypes.Role`, `role`, `roles`), the `roles` value also accepted as a JSON array (Azure AD
  app-roles). Role names are never space-split (unlike scopes - they can contain spaces).
- `RequirePolicy<TContext>(...)` - a named rule over the principal. Overloads: an
  `IAuthorizationPolicy` instance; a `policyName` resolved from DI (`GetServices<IAuthorizationPolicy>()`
  matched by `Name`, throws a wiring-error `InvalidOperationException` if absent); or a
  `(name, predicate)` inline (sync or async). `IAuthorizationPolicy` = `{ string Name;
  Task<bool> IsSatisfiedAsync(ClaimsPrincipal) }`; `DelegateAuthorizationPolicy` is the inline impl.
- `RequireAuthorization<TContext, TResource>(Func<TContext, TResource> resourceSelector)` -
  resource-based, resolves an app-registered `IAuthorizationHandler<TResource>`
  (`Task<bool> IsAuthorizedAsync(ClaimsPrincipal, TResource)`). The resource is derived from the
  **context** (topic/headers/route/query), because authorization runs before request mapping; a
  decision needing the typed request is done inside the handler instead.
- DI: `AddAuthorizationPolicy(policy)` / `AddAuthorizationPolicy(name, predicate)`.
- Benzene owns the enforcement mechanism; policy/handler *meaning* is the app's - no domain baked
  in, no external policy-engine (OPA/Cedar/`IAuthorizationService`) adapter shipped, only the seams.
- Tests: `test/Benzene.Core.Test/Auth/AuthorizationTest.cs` (real Kestrel host after
  `UseOAuth2Bearer`, mirroring `RequireScopeTest`): role match/miss/no-token, `roles` JSON-array,
  inline + by-name policy, resource-based same-tenant.

## Important conventions
- `Unauthorized` ("caller not authenticated") vs. `Forbidden` ("caller authenticated but not
  permitted") is the wire-contracts.md §3 distinction this whole feature exists to preserve - don't
  collapse the two. No principal at all is always `Unauthorized`, never `Forbidden`.
- This package does not itself produce a `ClaimsPrincipal` for anyone - it has no notion of Basic
  auth or bearer tokens. It only defines where a validated principal goes once a concrete mechanism
  produces one, and the shared short-circuit helper.
- `RequireScope` deliberately does **not** live here (see the design doc §8 Q3) - scopes are an
  OAuth2/JWT-specific concept (RFC 8693's `scope` claim, Azure AD's `scp`), not mechanism-agnostic,
  so it lives beside the middleware that actually produces scope-bearing tokens:
  `Benzene.Auth.OAuth2`.

## Dependencies on other Benzene packages
Abstractions (DI), Abstractions.MessageHandlers (`IMessageHandlerResultSetter<TContext>`,
`IMessageGetter<TContext>`), Core.MessageHandlers (`MessageHandlerResult`,
`MessageHandlerDefinition`), Results (`BenzeneResult`).
