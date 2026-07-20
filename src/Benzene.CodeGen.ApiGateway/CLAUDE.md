# Benzene.CodeGen.ApiGateway

## What this package does
Generates an AWS API Gateway extended-OpenAPI document (`openApi.yaml`, wrapped in
`# AUTOGEN START/END` markers) from a Benzene `EventServiceDocument`. For each HTTP-mapped topic it
emits the path, verbs, a CORS `OPTIONS` mock integration, security headers, and the
`x-amazon-apigateway-integration` VTL request/response templates that proxy API Gateway requests
into a Lambda-backed Benzene service.

## Key types
- `ApiGatewayBuilderV1 : ICodeBuilder<EventServiceDocument>` — the generator.
  `new ApiGatewayBuilderV1(string url)` uses generic defaults; `new ApiGatewayBuilderV1(ApiGatewayOptions)`
  configures it.
- `ApiGatewayOptions` — everything that must not be hard-coded per deployment:
  - `Url` — the backend integration URI token (emitted as `#{Url}#` for downstream substitution).
  - `AuthorizerName` — the custom authorizer applied to secured operations (in addition to `api_key`).
    **Null by default** → `api_key` only, no custom authorizer.
  - `UnauthenticatedTopics` — topics exempt from the custom authorizer (still `api_key`). Empty by default.
  - `AllowedHeaders` — the CORS `Access-Control-Allow-Headers` value. Minimal generic default
    (`Authorization,Content-Type,X-Api-Key`); add app-specific headers (e.g. `X-Tenant-Id`) here.
  - `IdentityHeaders` — extra request headers injected into the Lambda integration template, mapping a
    header name to a VTL value (typically authorizer-context claims, e.g.
    `["x-user-id"] = "$context.authorizer.userid"`). Empty by default.

## Important: no company coupling
This generator was originally hard-coded for one deployment (an Okta authorizer named "Elements",
a `PlatformTenantId`/licenses/subscriptions claim model, and `user:signup`/`user:migrate` as public
topics). That is all gone — those are now `ApiGatewayOptions` inputs with generic, empty defaults, so
the default output is company-free. Keep it that way: new deployment-specific values belong in
`ApiGatewayOptions`, never as string literals in the builder.

The CORS origin whitelisting still emits `#cors_allowed_origins#` / `#cors_localhost#` placeholder
tokens for a downstream token-substitution step — a templating convention, not a company value.

## Tests
- `test/Benzene.Core.Test/Autogen/CodeGen/ApiGateway/LambdaOpenApiBuilderTest.cs` — golden-file output
  (`Examples/GetUser.yaml`, `Examples/RbacTest.yaml`) for the default (company-free) output, plus
  option tests proving `AuthorizerName`/`UnauthenticatedTopics`/`IdentityHeaders`/`AllowedHeaders`
  apply and the default injects no authorizer or identity claims.
