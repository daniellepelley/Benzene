# Benzene.Auth.OAuth2

## What this package does
OAuth2 bearer token (JWT) validation as opt-in Benzene middleware (`UseOAuth2Bearer`), plus
scope-based authorization (`RequireScope`) - see `work/auth-middleware-design.md` §3.3. The one
package in the `Benzene.Auth.*` family with a third-party dependency (approved per the design doc
§5): `Microsoft.IdentityModel.JsonWebTokens` 8.19.2 (JWT parsing/validation -
`JsonWebTokenHandler`, the modern replacement for `JwtSecurityTokenHandler`) and
`Microsoft.IdentityModel.Protocols.OpenIdConnect` 8.19.2 (`ConfigurationManager<OpenIdConnectConfiguration>`
for JWKS discovery/caching/rotation). These are the same building blocks
`Microsoft.AspNetCore.Authentication.JwtBearer` uses internally - using them directly, without that
package, is what keeps this transport-agnostic (works identically behind Lambda, Azure Functions,
gRPC, Kafka, not only ASP.NET Core).

## Key types
- `OAuth2BearerOptions` - `Authority`/`JwksUri` (mutually exclusive; `Authority` is the full OIDC
  discovery URL, not just the issuer root - `Validate()` throws `ArgumentException` if both or
  neither are set), `ValidIssuers`/`ValidAudiences`/`ValidAlgorithms` (all required, no default -
  `Validate()` throws on an empty array), `ClockSkew` (default 2 minutes),
  `RequireHttpsMetadata` (default `true` - see below). `Validate()` runs at wire-up time
  (`UseOAuth2Bearer`), not on the first request - a misconfigured pipeline fails fast.
- `Extensions.UseOAuth2Bearer<TContext>(options)` - builds the shared, long-lived
  `JsonWebTokenHandler` + `TokenValidationParameters` + JWKS-caching
  `ConfigurationManager<OpenIdConnectConfiguration>` **once**, at wire-up time (not per request),
  and wires `OAuth2BearerMiddleware<TContext>`.
- `OAuth2BearerMiddleware<TContext>` - reads `Authorization: Bearer <token>`, calls
  `JsonWebTokenHandler.ValidateTokenAsync`, and either short-circuits `Unauthorized` (generic detail
  message - see "No detail leakage" below) or sets `Benzene.Auth.Core.AuthenticationHolder.Principal`
  from `TokenValidationResult.ClaimsIdentity` and calls `next()`. Registered scoped, not singleton
  - same reasoning as `Benzene.Auth.Basic.BasicAuthMiddleware<TContext>` (it carries the
  per-message `AuthenticationHolder`).
- `OAuth2ConfigurationManagerFactory` (internal) - builds the `ConfigurationManager<OpenIdConnectConfiguration>`:
  `OpenIdConnectConfigurationRetriever` (full discovery) for `Authority`, or
  `JwksOnlyConfigurationRetriever` (internal - fetches a bare JWKS document and wraps it in a
  minimal `OpenIdConnectConfiguration`) for `JwksUri`. Either way, wiring
  `TokenValidationParameters.ConfigurationManager` to the resulting manager is enough for
  `JsonWebTokenHandler.ValidateTokenAsync` to resolve signing keys and to auto-refresh on an
  unrecognized `kid` (verified empirically against a real generated RSA key + loopback JWKS server
  during implementation - no `IssuerSigningKeys`/`IssuerSigningKeyResolver` wiring needed on top).
- `ScopeClaims` (internal) - `GetGrantedScopes(principal)` normalizes both `scope` (RFC 8693,
  single space-delimited string) and `scp` (Azure AD - space-delimited string OR JSON array,
  depending on issuer) into one flat set (design doc §8 Q1's decision: support both).
- `Extensions.RequireScope<TContext>(anyOfScopes)` - `FuncWrapperMiddleware`-based (not a named
  class - no scoped constructor dependency beyond the holder it resolves inline). No principal →
  `Unauthorized`; principal but none of `anyOfScopes` granted → `Forbidden`. Lives here, not in
  `Benzene.Auth.Core`, because scopes are an OAuth2/JWT-specific concept (design doc §8 Q3).

## Algorithm-confusion prevention (security-critical - read before touching `ValidAlgorithms`)
`ValidAlgorithms` has **no default** and `OAuth2BearerOptions.Validate()` rejects an empty array.
This is deliberate, not an oversight: RFC 8725 §3.1 documents "algorithm confusion" attacks where a
validator that trusts whatever `alg` the token itself claims can be tricked - most infamously, an
attacker takes a service's own RSA *public* key (routinely not secret) and uses it as an HMAC
*secret* to forge an `HS256`-signed token that a permissive validator accepts as if it were the
`RS256` tokens the service actually issues. Requiring an explicit, non-empty allowlist here is what
closes that off; do not add a fallback that defaults `ValidAlgorithms` to "whatever the issuer
supports" or similar - that reintroduces exactly the hole this option exists to close.
`TokenValidationParameters.ValidateIssuerSigningKey`/`ValidateIssuer`/`ValidateAudience`/
`ValidateLifetime` are also all explicitly set `true` in `UseOAuth2Bearer` rather than left to
library defaults, for the same "don't silently under-validate" reasoning.

## No detail leakage on failure
A failed `ValidateTokenAsync` (bad signature, expired, wrong issuer/audience/algorithm) always
produces the same generic `"Invalid bearer token"` `Unauthorized` detail to the caller - the real
`TokenValidationResult.Exception` is logged server-side only (`ILogger`, category
`"Benzene.Auth.OAuth2"`), never returned. Distinguishable failure reasons in the response body are
an oracle an attacker can use to probe token shapes (design doc §3.3/§6) - don't add a more specific
detail message here, no matter how useful it seems for debugging; use server-side logs for that.

## RequireHttpsMetadata (test/local-dev escape hatch, defaults safe)
`OAuth2ConfigurationManagerFactory.Create` sets `HttpDocumentRetriever.RequireHttps` from
`OAuth2BearerOptions.RequireHttpsMetadata` (default `true`). Fetching the JWKS/OIDC discovery
document - the thing that establishes which keys are trusted - over plain HTTP is a
man-in-the-middle vector (a MITM in front of that fetch can substitute their own signing key), so
every real identity provider serves it over HTTPS and this stays required by default. The only
legitimate reason to set it `false` is a local-only fake JWKS endpoint in tests/dev (see
`test/Benzene.Core.Test/Auth/OAuth2BearerTest.cs`'s `FakeJwksServer`) - the same purpose ASP.NET
Core's own `JwtBearerOptions.RequireHttpsMetadata` serves. Never flip this in production; if you're
tempted to, the JWKS endpoint should be serving HTTPS instead.

## Dependencies on other Benzene packages
Auth.Core (`AuthenticationHolder`, `AuthResults`), Core.Middleware (`Use<TContext,TMiddleware>()`,
`FuncWrapperMiddleware<TContext>`), Http (`IHttpContext`, `IHttpRequestAdapter<TContext>`).
