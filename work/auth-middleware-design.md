# Authentication & Basic Scope Authorization Middleware — Design Proposal (2026-07-17)

**Status:** Approved (2026-07-17) — the open questions in §8 are resolved (each question now
records the decision) and implementation is in progress. §3.1/§3.2/§3.3 and §7 have been updated
in place to reflect the one decision that changed the design (`RequireScope` moved from
`Auth.Core` to `Auth.OAuth2`); the rest of the document is unchanged from the original proposal.

## 0. Framing (from the request)

Benzene services are commonly deployed behind a gateway (API Gateway, Azure Front Door, an ALB)
that may already terminate authentication — Benzene should not duplicate that. But not every
deployment has a gateway in front of it, and for those, authentication needs to be available at
the Benzene level as opt-in middleware. Two mechanisms were named as needed: **OAuth2 bearer
token (JWT) validation** and **HTTP Basic auth**, plus **scope-based authorization** (checking a
claim on an already-authenticated caller). Full RBAC, and integration with any specific
authorization-policy library, is explicitly out of scope. SOAP/WS-Security is explicitly declined.
This document also surveys other security gaps worth flagging (§6).

## 1. Current state (verified against actual code)

There is no authentication or authorization code anywhere in the repo today — this is a
greenfield feature, not a migration. A full-repo search turns up only:

- Roadmap placeholders with no design or code behind them: `work/azure-roadmap-1.0.md` repeatedly
  flags "Missing Azure authentication/authorization middleware" as open scope and has an unchecked
  `- [ ] OAuth 2.0 / OpenID Connect` line in a security checklist template;
  `work/observability-roadmap-1.0.md` has an unchecked `- [ ] Add authentication support (basic,
  bearer)` for a health-endpoint package. Both `work/aws-roadmap-1.0.md` and
  `work/website-live-assessment-2026-07-15.md` reference a `docs/cookbooks/auth-patterns.md`
  cookbook that does not exist yet.
- Unrelated hits: AWS API Gateway's own custom-authorizer event shape (a Lambda/IAM feature, not
  Benzene pipeline auth), the gRPC status mapper's `Unauthenticated`/`PermissionDenied` codes (pure
  status-code plumbing, see §4), and `Benzene.CodeGen.Cli.Core`'s `ConfluenceClient` setting a
  Basic `Authorization` header on its own outbound `HttpClient` call — internal CLI tooling,
  unrelated to the framework's request pipeline.
- `Benzene.Schema.OpenApi/CLAUDE.md` claims "Includes authentication schemes" — this is stale/
  aspirational; the only trace in that package's actual source is a commented-out
  `SecurityScheme` placeholder in `AsyncApi/Mapper.cs`.

No prior art to reconcile with. The design below is free to pick its own shape.

## 2. The key design insight: authentication is ordinary middleware, nothing more

Benzene already has the two things this feature needs:

1. **A status vocabulary that already means exactly this.** `wire-contracts.md` §3 defines
   `Unauthorized` ("caller not authenticated", 401 / gRPC `Unauthenticated`) and `Forbidden`
   ("caller authenticated but not permitted", 403 / gRPC `PermissionDenied`) as first-class
   statuses with mapping already wired for every transport. The new middleware needs to *return*
   these via the existing `BenzeneResult`/`IMessageHandlerResultSetter<TContext>` machinery — it
   invents nothing new on the wire.
2. **A precedent for exactly this shape of cross-cutting HTTP middleware**: `Benzene.Http`'s
   `CorsMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext`. It is
   transport-agnostic (works on ASP.NET Core, API Gateway, `Benzene.SelfHost.Http` — anything
   satisfying `IHttpContext`), takes a plain-POCO settings object, and is wired with the two-step
   `app.Register(x => x.AddSingleton(resolver => new XMiddleware(...))); return
   app.Use<TContext, XMiddleware<TContext>>();` pattern every `Use*` extension in this codebase
   follows. Auth middleware is the same shape: read something off the request, decide, either call
   `next()` or short-circuit with a result.

The one new mechanism needed is **where the authenticated identity goes** once validated, so a
later handler or a `RequireScope` check can read it. `TContext` must **not** grow an
`Authorization`/`Principal` property — `Benzene.Abstractions.Middleware/CLAUDE.md`'s "Context
purity" section is explicit that this is exactly the coupling the codebase avoids, in favor of a
small scoped-DI holder (the `PresetTopicHolder`/`PresetTopicMiddleware` pattern in
`Benzene.Core.MessageHandlers` is the worked example). §3.2 below applies that pattern here.

## 3. Concrete design

### 3.1 Package structure

Following the `Benzene.HealthChecks.Core` + `Benzene.HealthChecks` + satellite-packages precedent
(the closest fit in the codebase — `Core` holds pure contracts with zero third-party dependency;
concrete packages implement those contracts against one specific mechanism, each pulling in only
what it needs):

| Package | Contents | Third-party dependency |
|---|---|---|
| `Benzene.Auth.Core` | `AuthenticationHolder`, shared helpers (header parsing, the `Unauthorized`/`Forbidden` result-building helper) | none (BCL only) |
| `Benzene.Auth.OAuth2` | `UseOAuth2Bearer(...)` — JWT bearer validation against a JWKS endpoint — plus `RequireScope` (moved here per §8 Q3's decision: scopes are an OAuth2/JWT concept, not a mechanism-agnostic one) | `Microsoft.IdentityModel.JsonWebTokens` + `Microsoft.IdentityModel.Protocols.OpenIdConnect` (§5) |
| `Benzene.Auth.Basic` | `UseBasicAuth(...)` — RFC 7617 Basic auth against an app-supplied credential validator | none (BCL only) |

Both concrete packages depend only on `Benzene.Auth.Core` + `Benzene.Http` (for `IHttpContext`/
`IHttpRequestAdapter<TContext>`/`IBenzeneResponseAdapter<TContext>`), mirroring how
`Benzene.HealthChecks.Http` depends only on `HealthChecks.Core` plus what it wraps.

### 3.2 `Benzene.Auth.Core`: the scoped holder

```csharp
// AuthenticationHolder.cs — the scoped DI seam (Context purity pattern), not a TContext property.
// No interface: app code that wants to read the caller reads this type directly, same as
// PresetTopicHolder is read directly rather than through an abstraction.
public class AuthenticationHolder
{
    /// <summary>Set by whichever authentication middleware ran for this message; null if none did,
    /// or if the one that did run failed authentication.</summary>
    public ClaimsPrincipal? Principal { get; set; }
}
```

`ClaimsPrincipal`/`ClaimsIdentity` (BCL, `System.Security.Claims`) is the payload type — no new
Benzene-specific "principal" abstraction. It is the de facto standard shape for "an authenticated
identity plus claims" in .NET, every JWT/OAuth2 library already produces it, and inventing a
Benzene-specific wrapper would buy nothing.

Registration: each authentication middleware's own `Use*` extension does
`services.TryAddScoped<AuthenticationHolder>()` (idiom-identical to how each transport registers
`PresetTopicHolder` itself, not centrally) — a pipeline that never adds an authentication
middleware never even allocates a holder that anyone would look at.

### 3.3 `Benzene.Auth.OAuth2`: JWT bearer validation and `RequireScope`

```csharp
public class OAuth2BearerOptions
{
    /// <summary>The OIDC discovery URL (".../.well-known/openid-configuration"), used to fetch and
    /// auto-refresh the JWKS. Set this OR JwksUri, not both — most identity providers (Auth0,
    /// Cognito, Azure AD, Okta) expose full OIDC discovery; JwksUri is the escape hatch for ones
    /// that only publish a bare JWKS document.</summary>
    public string? Authority { get; set; }
    public string? JwksUri { get; set; }

    /// <summary>Every issuer this service trusts. Required — a token whose "iss" claim isn't in
    /// this list is rejected before signature validation even runs.</summary>
    public string[] ValidIssuers { get; set; } = Array.Empty<string>();

    /// <summary>Every audience this service accepts. Required for the same reason as ValidIssuers —
    /// a token minted for a different service must not be accepted here (the classic
    /// token-confusion mistake).</summary>
    public string[] ValidAudiences { get; set; } = Array.Empty<string>();

    /// <summary>Explicit signing-algorithm allowlist (e.g. "RS256"). Required, no default: a JWT
    /// validator that trusts whatever "alg" the token itself claims is vulnerable to algorithm-
    /// confusion attacks (RFC 8725 §3.1) — this library will not do that.</summary>
    public string[] ValidAlgorithms { get; set; } = Array.Empty<string>();

    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(2);
}

public static IMiddlewarePipelineBuilder<TContext> UseOAuth2Bearer<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, OAuth2BearerOptions options)
    where TContext : IHttpContext
{ /* ... */ }
```

Implementation wraps `Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler`
(`ValidateTokenAsync`) fed by a `Microsoft.IdentityModel.Protocols.ConfigurationManager
<OpenIdConnectConfiguration>` (built from `OpenIdConnectConfigurationRetriever` +
`HttpDocumentRetriever` when `Authority` is set, or a minimal static JWKS-only config when
`JwksUri` is set instead) — this is the exact machinery ASP.NET Core's own JWT bearer handler uses
internally, just without the `Microsoft.AspNetCore.*` coupling, which matters because Benzene
hosts the same pipeline behind Lambda/Azure Functions/gRPC/Kafka, not only ASP.NET Core.
`ConfigurationManager` handles JWKS caching and rotation on its own (refreshes on a `kid` it
doesn't recognize, rate-limited internally) — the middleware does not need to reimplement caching.

Middleware flow: read `Authorization` header → missing, not `Bearer `-prefixed, or empty token →
`Unauthorized` ("missing bearer token") → `ValidateTokenAsync` → failure (bad signature, expired,
wrong issuer/audience/algorithm) → `Unauthorized` with a generic detail message (never echo back
*why* validation failed in the response body — that's an oracle for attackers probing token
shapes; log the real reason server-side only) → success → build a `ClaimsPrincipal` from the
validated token's claims, set `AuthenticationHolder.Principal`, `next()`.

**`RequireScope`** (per §8 Q3's decision, this lives here rather than in `Auth.Core`: scopes are
specifically an OAuth2/JWT concept — RFC 8693's `scope` claim and Azure AD's `scp` convention —
not a mechanism-agnostic one, and putting it beside the middleware that actually produces
scope-bearing tokens is more honest than implying Basic auth commonly carries scopes too):

```csharp
// Extensions.cs (Benzene.Auth.OAuth2)
public static IMiddlewarePipelineBuilder<TContext> RequireScope<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, params string[] anyOfScopes)
    where TContext : IHttpContext
{
    app.Register(x => x.TryAddScoped<AuthenticationHolder>());
    return app.Use(resolver => new FuncWrapperMiddleware<TContext>("RequireScope", async (context, next) =>
    {
        var holder = resolver.GetService<AuthenticationHolder>();
        if (holder.Principal is null)
        {
            // No authentication middleware ran, or it ran and failed upstream — either way there
            // is no caller identity to check scopes against. This is Unauthorized, not Forbidden:
            // "no one is authenticated" and "someone is authenticated but lacks permission" are
            // different statuses (wire-contracts.md §3), and collapsing them would be a real
            // information-loss bug for API consumers debugging a 403 they can't explain.
            await SetResultAsync(resolver, context, BenzeneResultStatus.Unauthorized, "No authenticated caller");
            return;
        }

        // Per §8 Q1's decision: read BOTH conventions. "scope" is RFC 8693's single
        // space-delimited string; "scp" is Azure AD's convention and may appear as either a
        // space-delimited string or a JSON array, depending on issuer — normalize both into one
        // flat set of granted scope strings.
        var granted = ScopeClaims(holder.Principal);
        if (!anyOfScopes.Any(granted.Contains))
        {
            await SetResultAsync(resolver, context, BenzeneResultStatus.Forbidden,
                $"Missing required scope (any of: {string.Join(", ", anyOfScopes)})");
            return;
        }

        await next();
    }));
}
```

### 3.4 `Benzene.Auth.Basic`: RFC 7617 Basic auth

```csharp
public interface IBasicAuthCredentialValidator
{
    /// <summary>Validates a username/password pair. Returns the authenticated principal on
    /// success, null on failure — never throws for "wrong credentials" (that's an ordinary
    /// Unauthorized, not an application error). Implement this against whatever credential store
    /// the app actually uses (a secrets manager, an env var for a single service account, a user
    /// table) — this package deliberately ships no default implementation, so there is no
    /// hardcoded-credential footgun to accidentally deploy.</summary>
    Task<ClaimsPrincipal?> ValidateAsync(string username, string password);
}

public static IMiddlewarePipelineBuilder<TContext> UseBasicAuth<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, IBasicAuthCredentialValidator validator, string realm = "Benzene")
    where TContext : IHttpContext
{ /* ... */ }
```

Middleware flow: read `Authorization: Basic <base64>` → missing/malformed → `Unauthorized` (with a
`WWW-Authenticate: Basic realm="<realm>"` response header per RFC 7617 — worth doing properly
since some HTTP clients/browsers rely on it to prompt for credentials) → base64-decode → split on
the **first** `:` only (RFC 7617 explicitly allows `:` inside the password) → `ValidateAsync` →
null → `Unauthorized`; principal → set holder, `next()`.

## 4. What this does not solve (deliberately out of scope)

- **RBAC / policy engines / integration with a specific authorization library** — per the request.
  `RequireScope` (§3.3) is deliberately the ceiling of what this feature does for authorization;
  anything more structured (roles, resource-based policies, a rules engine) is an app concern
  layered on top of the `ClaimsPrincipal` this middleware already exposes.
- **SOAP / WS-Security** — declined per the request.
- **Issuing or refreshing tokens, or driving an OAuth2 login/consent flow** — `Benzene.Auth.OAuth2`
  only *validates inbound bearer tokens*; it is not an OAuth2 client, authorization server, or
  token-refresh helper for Benzene's own outbound HTTP clients. If Benzene services need to *call
  out* with an OAuth2 client-credentials token, that's a distinct feature (an outbound client
  decorator, analogous to the existing correlation-ID/trace decorators in
  `transport-bindings.md`), not part of this proposal.
- **mTLS, session/cookie auth, CSRF** — not addressed. CSRF specifically doesn't apply to Benzene's
  API-only request model (no browser-managed session cookies in play); mTLS is usually a
  host/gateway-level concern consistent with "security from where it's hosted" in the request, but
  see §6 for a lighter-weight note.
- **Rate limiting / throttling** — a separate concern; the `TooManyRequests` status already exists
  in the vocabulary for it, but no middleware implements it yet. Out of scope here.
- **Replay protection (nonce/jti tracking)** — short-lived bearer tokens plus `exp`/`nbf`
  validation are the standard mitigation for API auth; nonce tracking matters more for webhook
  signatures (see §6) than for OAuth2 bearer tokens, so it's not part of this design.

## 5. NuGet dependency ask

Per `AGENTS.md`: *"Do not add new NuGet dependencies without asking first."* This design needs one
new dependency, isolated to `Benzene.Auth.OAuth2` only (`Benzene.Auth.Core` and
`Benzene.Auth.Basic` need nothing beyond the BCL):

- `Microsoft.IdentityModel.JsonWebTokens` **8.19.2** — Microsoft's own, actively maintained JWT
  handling library; `JsonWebTokenHandler` is the modern, async-native, faster replacement for the
  older `System.IdentityModel.Tokens.Jwt`'s `JwtSecurityTokenHandler` (which Microsoft's own docs
  now point away from). AOT-compatible as of the 7.x/8.x line.
- `Microsoft.IdentityModel.Protocols.OpenIdConnect` **8.19.2** — supplies
  `ConfigurationManager<OpenIdConnectConfiguration>` for JWKS discovery, caching, and rotation.
  Transitively pulls in `Microsoft.IdentityModel.Protocols`, `Microsoft.IdentityModel.Tokens`, and
  `Microsoft.IdentityModel.Logging` — all from the same first-party family, no third-party chain.

These are the same building blocks `Microsoft.AspNetCore.Authentication.JwtBearer` uses
internally — using them directly, without that package, is what keeps `Benzene.Auth.OAuth2`
transport-agnostic (it must work identically hosted behind Lambda or Azure Functions, not only
ASP.NET Core).

## 6. Other security concerns worth flagging (beyond what was asked)

Roughly in priority order:

- **The mesh's own service-to-service traffic has no authentication today.** `Benzene.CloudService`'s
  `MeshAnnouncer` registers/heartbeats to a collector's `/benzene/invoke` over plain HTTP, and a
  collector accepts `mesh:register`/`mesh:heartbeat`/`mesh:traces` from anyone who can reach it.
  This wasn't part of the request, but it's the most concrete gap this investigation turned up: an
  unauthenticated collector can be fed a forged descriptor. A lightweight **API-key/shared-secret**
  mechanism (a fourth, much simpler package than OAuth2 or Basic — just a static header compared
  against a configured value) is the natural fit for this specific case, and is common generally
  for internal/service-to-service calls where a full OAuth2 exchange is overkill. Worth a follow-up
  design, not bundled into this one.
- **Inbound webhook signature validation (HMAC)** is a distinct, common third auth mechanism this
  request didn't name — validating an `X-Signature`/`X-Hub-Signature-256`-style HMAC over the raw
  body (the GitHub/Stripe pattern) for services that receive webhooks rather than calling an OAuth2
  provider. Different enough from bearer/Basic (no `Authorization` header, needs the *raw*
  pre-parsed body) that it deserves its own `Benzene.Auth.Hmac` package and its own short design,
  not a shoehorn into this one — flagging it here so it doesn't get lost.
- **Algorithm confusion / "none" algorithm** — addressed directly in §3.3's `ValidAlgorithms`
  design (no default, must be set explicitly) per RFC 8725 §3.1's guidance; called out here so it's
  visible as a deliberate decision, not an oversight.
- **`/benzene/spec` and the reserved `mesh` topic can leak schema/topology if left unauthenticated**
  on a service with no gateway in front of it. This isn't a new concern — `design-principles.md`
  §5.1 already documents blocking `/benzene/spec*` at a gateway as a supported pattern — but once
  this middleware exists, the docs should show composing it in front of those specific paths as the
  Benzene-level equivalent for services without a gateway. A `docs/cookbooks/auth-patterns.md`
  cookbook (already referenced-but-missing per §1) is the natural place; see open question 4 (§7).
- **Don't log the `Authorization` header.** Worth a one-line documented convention/warning when
  this ships (and a quick audit of any existing request-logging middleware for accidental full-
  header dumps) — a common, easy-to-miss footgun.
- **Multi-issuer support** is folded into the base OAuth2 design for free (`ValidIssuers` is
  already a list, §3.3) rather than treated as a separate future concern.

## 7. Recommended implementation order

1. `Benzene.Auth.Core` — `AuthenticationHolder` and the shared `Unauthorized`/`Forbidden`
   result-building helper only (`RequireScope` moved to step 3, per §8 Q3). Unit tests: holder
   defaults to null principal; the result-building helper produces the correct status/detail shape
   for both outcomes.
2. `Benzene.Auth.Basic` — simplest concrete package, validates the Core contracts end-to-end
   before taking on the JWT dependency. Unit tests: missing header, malformed base64, credentials
   with a colon in the password, validator returning null, `WWW-Authenticate` header present on
   401.
3. `Benzene.Auth.OAuth2` — the JWT/JWKS package, plus `RequireScope` (§3.3). Needs a fake JWKS
   endpoint (a loopback `HttpListener`, matching the pattern already used in
   `Benzene.CloudService.Probe`'s tests) or a locally-generated RSA keypair + hand-built token for
   unit tests: valid token accepted, expired token rejected, wrong issuer/audience rejected,
   unlisted algorithm rejected, malformed/missing header rejected, JWKS rotation (a `kid` not yet
   cached triggers a refresh) exercised at least once; `RequireScope` tested against both `scope`
   (space-delimited) and `scp` (string and array forms) claim shapes, plus the
   no-principal-yields-`Unauthorized`/wrong-scope-yields-`Forbidden` distinction.
4. Wire an example into `examples/` (an existing host — `Asp`, since it already hosts the Spec UI
   and is the fullest example per `examples/CLAUDE.md`) and write `docs/cookbooks/auth-patterns.md`
   (per §8 Q4's decision — in scope for this work, not a follow-up), closing the gap flagged in
   §1/§6.
5. Deferred (per §8 Q5's decision): revisit whether `Benzene.CloudService`'s `ICloudServiceBuilder`
   should gain an authentication hook, and the mesh API-key gap (§6), later — not part of this
   work.

## 8. Open questions — resolved (2026-07-17)

1. **Scope claim type**: **decided — support both.** `RequireScope` (§3.3) reads both `scope`
   (space-delimited) and `scp` (string or array) claims.
2. **Package naming**: **decided — `Benzene.Auth.*`.**
3. **Does `RequireScope` belong in `Auth.Core` or `Auth.OAuth2`**: **decided — `Auth.OAuth2`**
   (moved from the original §3.2 sketch; see §3.1's table and §3.3). `Auth.Core` now holds only the
   holder and the shared result-building helper.
4. **Is the `docs/cookbooks/auth-patterns.md` cookbook part of this work**: **decided — yes**, in
   scope now (§7 step 4).
5. **Should the mesh API-key gap (§6) get its own design doc now**: **decided — deferred.** Not
   part of this work; revisit later.
