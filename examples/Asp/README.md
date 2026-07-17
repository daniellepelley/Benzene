# Benzene ASP.NET Core Example

The ASP.NET Core host example — also where the Spec UI (`/spec-ui`) and the `spec` endpoint are wired. See `examples/CLAUDE.md` for how this fits among the other host examples.

## Running

```bash
dotnet run --project Benzene.Example.Asp --urls http://localhost:5000
```

- `GET /spec` — the derived Benzene spec document
- `GET /spec-ui` — a Swagger-UI-style browser for it
- Order handlers under `/orders` (see `examples/App/Benzene.Examples.App`)

## Authentication demo

`Startup.cs` also wires a `/protected/ping` route behind `UseOAuth2Bearer` + `RequireScope` (see [docs/cookbooks/auth-patterns.md](../../docs/cookbooks/auth-patterns.md) for the full pattern this demonstrates). It validates against a small self-contained fake identity provider (`DemoAuth/DemoJwtIssuer.cs`) rather than a real one, so you can try it with no external setup:

```bash
# 1. Mint a demo token with the "orders:read" scope
TOKEN=$(curl -s "http://localhost:5000/demo-token?scope=orders:read")

# 2. Call the protected route with it
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/protected/ping
# => {"message":"You are authenticated - RequireScope(\"orders:read\") let this request through.","scopes":["orders:read"]}

# Without a token at all: 401 Unauthorized
curl -i http://localhost:5000/protected/ping

# With a token that lacks the required scope: 403 Forbidden
BAD_TOKEN=$(curl -s "http://localhost:5000/demo-token?scope=orders:write")
curl -i -H "Authorization: Bearer $BAD_TOKEN" http://localhost:5000/protected/ping
```

The public routes above (`/spec`, `/spec-ui`, `/orders/*`) are untouched by any of this — the
protected route lives in its own `app.Map("/protected", ...)` branch (see `Startup.cs`), which is
also the pattern to follow if you need a real mix of public and protected routes in one app; see
the cookbook's "Protecting Only Some Routes" section for why a plain second `UseHttp` pipeline
alongside the public one wouldn't achieve the same isolation.

**`DemoAuth/` is demo-only scaffolding** — a real service has no equivalent of
`DemoAuthController`/`DemoJwtIssuer`; it points `OAuth2BearerOptions.Authority`/`JwksUri` at an
actual identity provider (Auth0, Cognito, Azure AD, ...) instead.
