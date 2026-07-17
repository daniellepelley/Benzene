# Benzene.Auth.Basic

## What this package does
RFC 7617 HTTP Basic authentication as opt-in Benzene middleware (`UseBasicAuth`). The simplest of
the `Benzene.Auth.*` family (see `work/auth-middleware-design.md`) - validates this against
`Benzene.Auth.Core`'s contracts end-to-end before `Benzene.Auth.OAuth2` takes on the JWT
dependency. No third-party dependency beyond the BCL.

## Key types
- `IBasicAuthCredentialValidator` - `Task<ClaimsPrincipal?> ValidateAsync(username, password)`.
  Ships with no default implementation on purpose - hardcoding a credential store here would be a
  footgun waiting to be deployed. Implement it against whatever the app actually uses.
- `BasicAuthMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext` - reads
  `Authorization: Basic <base64>`, decodes, splits on the **first** `:` only (RFC 7617 explicitly
  allows `:` inside the password - a naive `Split(':')` would corrupt it), and calls the validator.
  Registered **scoped**, not singleton like `Benzene.Http.Cors.CorsMiddleware` - it carries
  `Benzene.Auth.Core.AuthenticationHolder`, which is itself scoped to one message, so the
  middleware instance holding it must be re-resolved (and its constructor re-run) per message too.
  A cached singleton would capture one holder instance for the app's entire lifetime.
- `Extensions.UseBasicAuth<TContext>(validator, realm = "Benzene")` - registers
  `AuthenticationHolder` scoped in this extension (not centrally - the Context Purity pattern, see
  `Benzene.Auth.Core/CLAUDE.md`) and wires the middleware.

## Middleware flow
1. Missing `Authorization` header, or not `Basic `-prefixed (case-insensitive scheme match) →
   `Unauthorized` + `WWW-Authenticate: Basic realm="<realm>"`.
2. Malformed base64, or no `:` in the decoded credentials → same.
3. `ValidateAsync(username, password)` returns `null` → same.
4. `ValidateAsync` returns a principal → `AuthenticationHolder.Principal` is set, `next()` runs.

`WWW-Authenticate` is set on **every** `Unauthorized` this middleware produces, not only the
missing-header case - RFC 7617 says a 401 to a request without valid credentials should carry the
challenge, and that's what makes browsers/HTTP clients actually prompt for credentials.

## Dependencies on other Benzene packages
Auth.Core (`AuthenticationHolder`, `AuthResults`), Core.Middleware (`Use<TContext,TMiddleware>()`),
Http (`IHttpContext`, `IHttpRequestAdapter<TContext>`, `IBenzeneResponseAdapter<TContext>`).
