# Adopter feature requests — an HTTP API on AWS Lambda

**Status: proposed, 2026-11-19. Not yet assessed.** These are the enhancements an adopter needs to move
an existing HTTP API (a "Tasks" CRUD service on API Gateway and Lambda) onto Benzene. Each item below
is a self-contained request: what the code does today (with file references), what is missing, the
requirements, a suggested API, acceptance tests, and whether the cross-language spec changes first.
It is written to be picked up one item at a time, by a person or an agent, and taken through
**assess → plan → implement**.

Evidence is from benzene-dotnet `main` at `4bf13aa` and the spec at `spec-1.2.0`. Paths are relative
to benzene-dotnet unless they start with `docs/specification/`, which is this repo. Only .NET was
checked; whether Go, TypeScript and Python have the same gaps is not known.

## Contents

- [What already exists](#what-already-exists)
- [How to take an item through](#how-to-take-an-item-through)
- [Summary](#summary)
- [P0: blocks the migration](#p0-blocks-the-migration) — FR-1 to FR-5
- [P1: needed to keep today's behaviour](#p1-needed-to-keep-todays-behaviour) — FR-6 to FR-10
- [P2: lower priority](#p2-lower-priority) — FR-11 to FR-14
- [Build order](#build-order)
- [Definition of done for the whole list](#definition-of-done-for-the-whole-list)

## What already exists

Benzene has more than its docs suggest. None of these is a gap, and none should be rebuilt:

| Capability | Where |
|---|---|
| Rate limiting, in-process (fixed window, token bucket, payload size, any `System.Threading.RateLimiting` limiter) | `src/Benzene.RateLimiting/Extensions.cs` |
| Header-based version selection (missing version → default handler; unknown → `not-found`) | `src/Benzene.Core.MessageHandlers/VersionSelector.cs` |
| Payload casting between versions | `src/Benzene.Core.Versioning` |
| Newtonsoft.Json serializer | `src/Benzene.NewtonsoftJson` |
| Idempotency, with a DynamoDB store | `src/Benzene.Idempotency`, `src/Benzene.Idempotency.DynamoDb/DynamoDbIdempotencyStore.cs` |
| Lambda hosting bootstrap | `src/Benzene.Aws.Lambda.Hosting/AwsLambdaBootstrap.cs` |
| Warm-up during the Lambda init phase (`AddBenzeneWarmUp`, `IWarmUpTask`) | `src/Benzene.Aws.Lambda.Core/AwsLambdaHost.cs:55` |
| JWT bearer validation and Basic auth, setting a `ClaimsPrincipal` | `src/Benzene.Auth.OAuth2/OAuth2BearerMiddleware.cs`, `src/Benzene.Auth.Basic` |
| Scope, role and policy checks with the 401/403 split | `src/Benzene.Auth.OAuth2/Extensions.cs:106-130`, `src/Benzene.Auth.Core/AuthorizationExtensions.cs` |
| CORS | `src/Benzene.Http/Cors/CorsMiddleware.cs` |
| Result status → HTTP status mapping, replaceable (`IHttpStatusCodeMapper`) | `src/Benzene.Http/DefaultHttpStatusCodeMapper.cs` |
| RFC 9457 problem documents for failures, replaceable (`IResponsePayloadMapper`) | `src/Benzene.Core.MessageHandlers/Response/DefaultResponsePayloadMapper.cs` |
| One trace event per invocation to a pluggable exporter (`UseMeshTrace`, `IMeshTraceExporter`) | `src/Benzene.Mesh.Wire/Extensions.cs:36-53` |
| OpenAPI 3.0 generation | `src/Benzene.Schema.OpenApi/OpenApi/OpenApiDocumentBuilder.cs` |

## How to take an item through

1. **Assess.** Confirm the "Today" evidence still holds on current `main`, and settle the item's open
   questions. Some items may turn out to be docs, not code.
2. **Spec first, where marked.** An item marked **Spec change: yes** changes an observable contract.
   Make the change here in `docs/specification/**` with conformance fixtures, release it, and let the
   ports re-vendor. Only then implement the .NET side (AGENTS.md: "the specification is the product").
3. **Plan.** Write the item's implementation plan in benzene-dotnet `work/`, its home under
   [`work/README.md`](README.md)'s one-home rule, linking back here.
4. **Implement.** One PR per item, covering every host it names: API Gateway REST (v1), API Gateway
   HTTP API (v2) and ASP.NET Core. Follow benzene-dotnet's AGENTS.md, with tests through the
   `*.TestHelpers` packages on each host.
5. **Close.** Update benzene-dotnet's `docs/capability-matrix.md` (`capability-scribe`), this repo's
   `docs/capabilities.md` if a port matrix changed (`port-aligner`), and mark the item done here with
   the commit. When every item is done or declined, archive this file.

## Summary

| ID | Request | Priority | Today | Spec change | Hosts |
|---|---|---|---|---|---|
| FR-1 | Repeated query keys bind as arrays | P0 | **Done 2026-11-19** (benzene-dotnet `131898d`) | No (maybe a binding note) | v1, v2, ASP.NET |
| FR-2 | Base path stripping and the request URL | P0 | **Done 2026-11-19** (benzene-dotnet `81361e4`) | No | v1, v2 (ASP.NET: URL only) |
| FR-3 | Response headers from handler results | P0 | **Done 2026-11-19** (benzene-dotnet `605225f`) | No | v1, v2, ASP.NET |
| FR-4 | Caller identity from the API Gateway authorizer | P0 | **Done 2026-11-19** (benzene-dotnet `9d7096d`) | No | v1, v2 |
| FR-5 | Per-endpoint authorization | P0 | **Done 2026-11-19** (benzene-dotnet `8fcf5dd`) | No | v1, v2, ASP.NET |
| FR-6 | ETag and conditional requests (304/412/428) | P1 | **Done 2026-11-19** (spec `1.3.0`, benzene-dotnet `8236941`) | **Yes** (released) | v1, v2, ASP.NET |
| FR-7 | Rate and concurrency limits across instances | P1 | Partial: per-instance only | No | all |
| FR-8 | One event per request to a durable sink | P1 | Partial: trace on the envelope route only | Maybe | all |
| FR-9 | Keep the current error body format | P1 | **Exists** (swap point); one gap | No, but see conformance | all |
| FR-10 | Lambda SnapStart | P1 | Gap; init warm-up exists | No | Lambda |
| FR-11 | Strict header versioning | P2 | Partial: selector replaceable | No | all |
| FR-12 | Configurable JSON options | P2 | Partial: constructor only | No | all |
| FR-13 | OpenAPI for all of the above | P2 | Partial | No | — |
| FR-14 | Docs fixes | P2 | **Done 2026-11-19** (benzene-dotnet `be33941`), one follow-up after FR-7 | No | — |

---

## P0: blocks the migration

### FR-1 — Repeated query-string keys

**Done 2026-11-19, benzene-dotnet `131898d`.** A repeated key binds every value to a collection
property (`T[]`, `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`,
`IReadOnlyCollection<T>`) and its first value to a scalar, on API Gateway v1 and v2, ASP.NET Core,
Google Cloud Functions and Azure Functions.

The open questions were settled as follows:
- An absent key leaves the property as the body set it.
- The comma form doesn't split on v1 or ASP.NET Core.
- A value that won't convert is `bad-request`, as a scalar already was, not a `validation-error`.

Azure Functions changed behaviour: it used to bind `"a,b"` to a scalar.

Still open:
- the benchmark run;
- the optional one-sentence binding note in `transport-bindings.md`.

The plan is archived as `benzene-dotnet/work/archive/fr-1-repeated-query-keys-plan-2026-11.md`.

The original request:

**Today.** Every host keeps the first value of a repeated key and drops the rest, so `?id=a&id=b`
binds only `a`:

- v1 reads the multi-value map but keeps only the first value,
  `src/Benzene.Aws.Lambda.ApiGateway/QueryStringFirstWinsMapper.cs:29-31`:
  `.ToDictionary(x => x.Key, x => x.Value?.FirstOrDefault())`.
- v2 receives repeated keys comma-joined and keeps the first segment, same file line 44:
  `x.Value?.Split(',')[0]`.
- ASP.NET Core does the same, `src/Benzene.AspNet.Core/AspNetRequestEnricher.cs:50`:
  `x.Value.First()`.
- A collection property can't bind from the query. Values reach
  `src/Benzene.Core/Helper/DictionaryUtils.cs:184-217` as single strings, and the fallback
  `Convert.ChangeType` (line 255) cannot produce a `string[]` or `List<T>`.

The behaviour is documented as intended (`src/Benzene.Aws.Lambda.ApiGateway/CLAUDE.md:125-130`).
`work/1.0-release-plan.md` item 4.6 (lines 594-607) records full multi-value support as deferred until
after 1.0. That entry also says "last-value-wins", but the code is first-value-wins (FR-14).

**Why it blocks.** List endpoints filter by repeated keys (`GET /tasks?status=open&status=blocked`).
Today the second filter is silently ignored and the endpoint returns the wrong rows.

**Requirements.**
1. A request property of type `string[]`, `List<T>`, `IReadOnlyList<T>` or `IEnumerable<T>`, for any
   scalar `T` the binder already converts (including `Guid`, enums and `Nullable<T>`), binds every
   value of that query key, in request order.
2. A scalar property keeps today's first-value-wins behaviour, so existing services don't change.
3. v1 takes values from `MultiValueQueryStringParameters`. v2 splits the comma-joined value, the only
   form API Gateway HTTP APIs deliver; record that a literal comma inside a v2 value cannot be told
   apart. ASP.NET takes `StringValues`.
4. An absent key leaves a collection property as the binder's default (empty or `null`; decide
   which), not a one-element collection holding `null`.
5. A value that fails to convert is a `validation-error` naming the key, as a scalar does today.
6. Multi-value request headers (also dropped today, `CLAUDE.md:114-119`) are out of scope. Note it
   as a follow-up.

**Suggested approach.** Thread a multi-value query view
(`IReadOnlyDictionary<string, IReadOnlyList<string>>`) through the enrichers beside the existing
single-value one. Bind collection-typed properties from it. Keep the single-value getter contract as
it is, so nothing else changes.

**Acceptance tests,** on each of v1, v2 and ASP.NET:
- `?id=a&id=b` into `string[] Id` gives `["a","b"]`, in order.
- `?n=1&n=2` into `List<int> N` gives `[1,2]`.
- `?status=Open&status=Blocked` into an enum list binds both values.
- `?id=a&id=b` into a scalar `string Id` gives `"a"`, as today.
- A missing key gives the agreed default.
- `?n=1&n=x` into `List<int>` gives `validation-error` naming `n`.

**Spec change.** No. Query binding is a port concern. Optionally add one sentence to
`docs/specification/transport-bindings.md` saying a repeated query key binds to a collection, so the
ports agree.

**Open questions.** Should an empty collection be `[]` or `null`? Should
`?ids=a,b` (comma form) also split on v1 and ASP.NET? Recommend not: only v2 needs it.

### FR-2 — Base path stripping and the request URL

**Done 2026-11-19, benzene-dotnet `81361e4`.**
- `UseBasePath("/tasks")`, first in the HTTP pipeline, strips the prefix on API Gateway v1 and v2,
  ASP.NET Core and Azure Functions. Azure's `/api` is now one line. A request outside the base path
  is `not-found`.
- `IRequestUrl` is injectable through `UseRequestUrl(...)`, which `UseBasePath` includes. It gives
  scheme, host, base path, path and `Absolute(...)`.
- `X-Forwarded-Host` is read only when `TrustForwardedHost` is set, and `PublicBaseUrl` pins both
  scheme and host.

The open questions were settled as follows:
- The base path is set per pipeline.
- API Gateway stages are not stripped automatically. No verified event shows when a stage is in the
  path, so a stage that appears is written as part of the base path (`UseBasePath("/prod/tasks")`).

Not done: folding the OIDC package's internal `RequestUrl` into `IRequestUrl`. The plan is archived
as `benzene-dotnet/work/archive/fr-2-base-path-and-request-url-plan-2026-11.md`.

The original request:

**Today.**
- On Lambda, routing matches the raw request path with nothing stripped. v1 uses
  `ApiGatewayProxyRequest.Path` (`ApiGatewayMessageTopicGetter.cs:32`,
  `ApiGatewayRequestEnricher.cs:40`). v2 uses `RequestContext.Http.Path` (`ApiGatewayV2Context.cs:53`).
  Nothing in the HTTP or API Gateway packages handles a stage, `PathBase` or a custom-domain mapping.
  So a service mounted at `/tasks` behind a custom domain, or reached through a non-default stage,
  has to repeat the prefix in every `[HttpEndpoint]`, and that breaks when the mapping changes.
- On ASP.NET Core, `Request.Path` already excludes `PathBase` (`AspNetHttpRequestAdapter.cs:107`), so
  `app.Map("/tasks", …)` strips the prefix.
- No host gives a handler the request's scheme, host or base URL. `src/Benzene.Http/HttpRequest.cs`
  carries only `Method`, `Path` and `Headers`. The one URL builder is internal to the mesh package
  (`src/Benzene.Mesh.Auth.Oidc/RequestUrl.cs:9`, `internal static class`).

**Why it blocks.** The service is mounted under a base path. It also builds absolute links (the
`Location` of a created task, pagination `next` links) and needs the public base URL to do it.

**Requirements.**
1. A configurable base path (`/tasks`) on each Lambda host, stripped before routing. A request outside
   the base path is `not-found`.
2. Optionally, strip the API Gateway stage automatically when the path carries it. v1 REST APIs on
   the default execute-api domain include the stage in `Path` only in some setups; verify against
   real events before deciding.
3. A host-neutral request-URL abstraction a handler can inject: scheme, host, base path and path,
   plus a helper that builds an absolute URL from a relative one.
   - On Lambda it comes from `Host` / `X-Forwarded-Host` and `X-Forwarded-Proto`, then the event's
     `requestContext.domainName`.
   - On ASP.NET it comes from `HttpRequest.Scheme`, `Host` and `PathBase`, honouring
     `UseForwardedHeaders`.
4. Forwarded headers are trusted only when configured, so a caller can't spoof the host in
   generated links.

**Suggested API.**
```csharp
app.UseHttp(http => http.WithBasePath("/tasks"));   // Lambda; ASP.NET keeps app.Map

public interface IRequestUrl
{
    string Scheme { get; }
    string Host { get; }
    string BasePath { get; }   // "/tasks"
    string Path { get; }       // "/123", after stripping
    Uri Absolute(string relative);   // Absolute("/123") → https://api.example.com/tasks/123
}
```

**Acceptance tests.**
- With base path `/tasks`, `GET /tasks/123` routes to `[HttpEndpoint("GET", "/{id}")]` and
  `GET /other/123` is `not-found`, on v1 and v2.
- `IRequestUrl.Absolute("/123")` gives the public URL on v1, v2 and ASP.NET, both with and without
  forwarded headers.
- An untrusted `X-Forwarded-Host` is ignored unless configured.

**Spec change.** No.

**Open questions.** Should the base path be per host or per pipeline? Should `IRequestUrl` absorb the
mesh package's internal `RequestUrl`? Recommend yes, to keep one implementation.

### FR-3 — Response headers from handlers

**Done 2026-11-19, benzene-dotnet `605225f`.** `BenzeneResult.Created(task).WithHeader("Location", …)`
writes the header on every HTTP host, and into the response envelope's `headers` over
`/benzene/invoke`. Transports with no response headers ignore it.

Requirement 2's precedence was settled differently from the suggestion below: **a header the pipeline
set wins**. CORS, written before the handler, and the rendered `content-type` are never replaced; the
handler adds what the pipeline didn't set. The reason: CORS is the only middleware header that
coexists with a handler result. Writing result headers over it would have replaced CORS values on
API Gateway and the envelope, duplicated them on ASP.NET Core, and thrown on Azure Functions.

`WithHeader` rejects CR, LF and NUL and non-token names. Not done: surfacing received headers on a
Benzene client's result (optional). The plan is archived as
`benzene-dotnet/work/archive/fr-3-response-headers-plan-2026-11.md`.

The original request:

**Today.** A handler result can't carry headers.
- `src/Benzene.Abstractions/Results/IBenzeneResult.cs:7-35` has only `Status`, `IsSuccessful`,
  `PayloadAsObject` and `Errors`.
- `BenzeneResult.Created<T>(payload)` (`src/Benzene.Results/BenzeneResult.cs:185`) has no
  `Location`.
- The HTTP mapping sets only the status code (`src/Benzene.Http/HttpStatusCodeResponseHandler.cs:47`).
- Headers are set only by middleware through `IBenzeneResponseAdapter.SetResponseHeader`: CORS,
  Basic auth's `WWW-Authenticate`, OIDC's `Location`, and rate limiting's `Retry-After`.

**Why it blocks.** `POST /tasks` must answer `201 Created` with `Location: /tasks/{id}`. FR-6 needs
`ETag`, and other endpoints return `Cache-Control` and `Content-Location`.

**Requirements.**
1. A handler can attach response headers to its result, on success and failure alike.
2. Each HTTP host writes them to the response, after the status mapping and before or merged with
   middleware headers. Settle the precedence: recommend that the handler wins over defaults and
   middleware that runs later wins over the handler, so CORS and security headers can't be
   suppressed by accident.
3. On a non-HTTP transport (a queue, the envelope endpoint) the headers are ignored, or carried in
   the envelope's `headers` if the spec allows. Decide which and document it.
4. A relative `Location` can be made absolute through FR-2's `IRequestUrl`.
5. Existing results and `IBenzeneResult` implementations keep compiling. Add a default member or a
   separate interface; don't change the existing signatures.

**Suggested API.**
```csharp
return BenzeneResult.Created(task)
    .WithHeader("Location", $"/tasks/{task.Id}")
    .WithHeader("ETag", etag);

public interface IHasResponseHeaders { IReadOnlyDictionary<string, string> ResponseHeaders { get; } }
```

**Acceptance tests.**
- On v1, v2 and ASP.NET, a `Created` result with a `Location` returns 201 with that header.
- A header on a failure result is written too.
- When CORS middleware and a handler both set `Access-Control-Allow-Origin`, the agreed precedence
  holds.
- An envelope-route invocation doesn't fail because of headers.

**Spec change.** No, unless headers should travel inside the envelope. In that case it is an addition
to `docs/specification/wire-contracts.md` §1 and needs fixtures.

### FR-4 — Caller identity from the API Gateway authorizer

**Done 2026-11-19, benzene-dotnet `9d7096d`.** The new package `Benzene.Auth.Aws.ApiGateway` adds
`UseApiGatewayAuthorizerIdentity(...)` on REST and HTTP API pipelines. It turns the authorizer
context into the caller, so `RequireScope`, `RequireRole` and `RequirePolicy` work unchanged:
- REST API: Cognito claims, and a Lambda authorizer's context with its `principalId`;
- HTTP API: JWT claims and scopes, Lambda context, and IAM.

Options set the name claim and the role claims (`cognito:groups`). A caller another authenticator
already set is kept. `ScopeClaims` moved to `Benzene.Auth.Core` as a public type. The docs carry
the trust warning (no function URL, no direct invoke).

The plan is archived as `benzene-dotnet/work/archive/fr-4-authorizer-identity-plan-2026-11.md`.

The original request:

**Today.** A caller exists only if Benzene authenticated them itself.
- `UseOAuth2Bearer` validates the JWT and sets the principal (`OAuth2BearerMiddleware.cs:64-107`).
- Basic auth sets it through `IBasicAuthCredentialValidator`.
- Both store it in the scoped `AuthenticationHolder` (`src/Benzene.Auth.Core/AuthenticationHolder.cs:27-33`).
- Nothing reads `requestContext.authorizer`. The only authorizer code is the custom-authorizer Lambda
  app, which produces IAM policies.
- The gap was recorded in `work/archive/cloud-review-2026-07/03-aws-apigateway.md:31-35`
  ("`requestContext` (authorizer claims, identity, stage) not surfaced to handlers"), and nothing
  later closes it.

**Why it blocks.** API Gateway already authenticates the caller, with a JWT authorizer on v2 or a
Lambda or Cognito authorizer on v1. Validating the token a second time in the function is redundant,
needs the signing keys in the function, and doesn't work for a Lambda authorizer's custom context.

**Requirements.**
1. Middleware that builds a `ClaimsPrincipal` from the event's authorizer context and puts it in
   `AuthenticationHolder`, so `RequireScope`, `RequireRole`, `RequirePolicy` and FR-5 work unchanged:
   - v1: `requestContext.authorizer.claims` (Cognito user pools) and the Lambda-authorizer context
     map, including `principalId`.
   - v2: `requestContext.authorizer.jwt.claims` and `jwt.scopes`, and
     `requestContext.authorizer.lambda`.
2. Claim mapping is configurable: which claim is the name, which holds roles, and which holds scopes.
   Scopes arrive as a space-separated `scope` claim, an `scp` claim, or v2's `jwt.scopes` array.
3. No authorizer context means no principal, so `RequireScope` answers 401 as today.
4. The docs say plainly that this trusts API Gateway, so the function must not be reachable any other
   way (a function URL, direct invoke).
5. Optionally, a host-neutral way for a handler to read the caller (`ICurrentCaller`) instead of
   reaching into `AuthenticationHolder`.

**Suggested API.**
```csharp
app.UseApiGatewayAuthorizerIdentity(o =>
{
    o.ScopeClaim = "scope";          // or "scp"; v2 jwt.scopes is read automatically
    o.RoleClaim  = "cognito:groups";
});
```

**Acceptance tests.**
- A v2 event with JWT authorizer claims and scopes produces a principal, and `RequireScope("tasks.read")`
  passes or fails on the scopes.
- A v1 Cognito event and a v1 Lambda-authorizer context both produce a principal.
- An event with no authorizer gives 401 through `RequireScope`.
- The claim-mapping options change which claims become name, role and scope.

**Spec change.** No.

### FR-5 — Per-endpoint authorization

**Done 2026-11-19, benzene-dotnet `8fcf5dd`.** Requirements are declared with `[RequireScope]`,
`[RequireRole]`, `[RequirePolicy]` and `[AllowAnonymous]` (`Benzene.Auth.Core.Endpoints`), or with
`options.ForTopic(...)`, and enforced by `UseEndpointAuthorization()`:
- It is an outer middleware, so it authorizes before the body is read: a malformed body to a
  protected endpoint is 401, not 400.
- A handler with no attribute needs a caller by default.
- Benzene's spec, ping and health topics are anonymous by default.
- A topic with no handler still 404s.
- It works on every transport it's added to.

Not done: OpenAPI `security` per operation, which is part of FR-13. The plan is archived as
`benzene-dotnet/work/archive/fr-5-endpoint-authorization-plan-2026-11.md`.

The original request:

**Today.** Authorization applies to the whole pipeline.
- `RequireScope` (`src/Benzene.Auth.OAuth2/Extensions.cs:106-130`) and `RequireRole`, `RequirePolicy`
  and `RequireAuthorization` (`src/Benzene.Auth.Core/AuthorizationExtensions.cs:39-159`) are
  pipeline middleware.
- `[HttpEndpoint]` (`src/Benzene.Http/HttpEndpointAttribute.cs`) has only `Method` and `Url`.
- There is no `[AllowAnonymous]`, no scope attribute and no branch-by-topic.
- The documented workaround is an ASP.NET `app.Map` branch (`docs/cookbooks/auth-patterns.md:251-292`),
  which has no Lambda equivalent. The same doc says sibling `UseHttp` pipelines "can't split routes
  between them".

**Why it blocks.** The Tasks API needs different scopes per endpoint (`tasks.read` for `GET`,
`tasks.write` for `POST`, `PUT` and `DELETE`) and a few anonymous endpoints, all behind one function.

**Requirements.**
1. Declarative requirements on a handler class: scopes (any of), roles and named policies, plus an
   explicit anonymous opt-out.
2. Enforced after routing resolves the handler and before the handler runs, with the same 401/403
   split as `RequireScope`: no caller is 401, a missing requirement is 403. This should be one
   middleware, placed once.
3. A pipeline-wide default ("authenticated unless `[AllowAnonymous]`") is available, so a forgotten
   attribute fails closed.
4. Works for every transport the handler is reachable on, not just HTTP, since a handler can also be
   reached through the envelope endpoint. A transport with no caller must fail closed.
5. The requirements are visible to FR-13 (OpenAPI security) and, ideally, to the mesh/self-description
   output.
6. An equivalent registration-time API (`.ForTopic("tasks:create").RequireScope(...)`) for handlers
   that prefer not to use attributes.

**Suggested API.**
```csharp
[Message("tasks:create")]
[HttpEndpoint("POST", "/")]
[RequireScope("tasks.write")]
public class CreateTaskHandler : IMessageHandler<CreateTask, TaskDto> { … }

[AllowAnonymous]
public class HealthHandler : … { … }

app.UseEndpointAuthorization(o => o.RequireAuthenticatedByDefault = true);
```

**Acceptance tests.**
- Two handlers with different scopes in one pipeline: the right one passes and the other gets 403.
- No caller gives 401.
- `[AllowAnonymous]` passes with no caller.
- With the default on, a handler without attributes needs a caller.
- The same handler reached through the envelope endpoint enforces the same requirements.
- All of the above on v1, v2 and ASP.NET.

**Spec change.** No. Exposing requirements in the contract document would be a later spec addition.

---

## P1: needed to keep today's behaviour

### FR-6 — ETag, If-Match and If-None-Match

**Done 2026-11-19.** Decided: two new statuses (option a), and 304 as an HTTP-binding rule, not a
status.
- **Spec 1.3.0** (`e5f2ac7`): `precondition-failed` (412) and `precondition-required` (428), both
  gRPC `FailedPrecondition`, plus the 304 rule (wire-contracts §4.1). Fixtures updated.
- **benzene-dotnet** (`8236941`), pinned to `spec-1.3.0`:
  - both statuses everywhere;
  - `ConditionalGetResponseHandler`: 304 when a successful `GET`'s `ETag` result header matches
    `If-None-Match`;
  - `Preconditions.CheckIfMatch` and `EntityTags`;
  - `If-Match`/`If-None-Match` bound onto `IfMatch`/`IfNoneMatch` request properties.

**Still open: Go, TypeScript and Python** re-pin to `spec-1.3.0` and implement the two statuses.
The 304 rule is a MAY, so it's optional for them. Until then they map the statuses to 500 /
`Internal` (`docs/capabilities.md` records this as staged).

The original request:

**Today.** There is no support. Searching `src` finds no HTTP conditional-request code, only S3
`ETag` fields. The status vocabulary is a closed set of 17
(`docs/specification/wire-contracts.md` §3, lines 161-191; .NET `src/Benzene.Results/BenzeneResultStatus.cs:13-29`).
None maps to 304, 412 or 428 (§4.1, lines 202-222). An inbound 304, 412 or 428 maps back to
`unexpected-error` (lines 228-232).

**Why it is needed.** The current API supports optimistic concurrency (`PUT`/`DELETE` with
`If-Match`, 412 on mismatch, 428 when the header is required and missing) and conditional `GET`
(`If-None-Match`, 304).

**Requirements.**
1. Statuses for "precondition failed" (412) and "precondition required" (428), with HTTP mappings
   both ways.
2. 304 Not Modified for a conditional `GET`. It has no body, and it is a success.
3. A handler can read `If-Match` and `If-None-Match` without touching raw host types, and can return
   an `ETag` (through FR-3).
4. Optionally, a helper that compares a handler-supplied ETag with the request's preconditions and
   short-circuits. Strong comparison for `If-Match`, weak for `If-None-Match` (RFC 9110 §13).
5. Problem types for the two new failures in the registry (wire-contracts §3.1).

**Decided 2026-11-19: option (a)** for the two precondition statuses. Whether 304 is a vocabulary
entry (`not-modified`) or an HTTP-binding rule is still open, and is the first thing to settle when
FR-6 is planned. The options were:
- **(a) Extend the shared vocabulary** with `precondition-failed`, `precondition-required` and
  `not-modified`. This is a spec change every port implements. `precondition-failed` is meaningful
  off HTTP as optimistic concurrency.
- **(b) Application-defined statuses,** mapped by a custom `IHttpStatusCodeMapper`, with no spec
  change. Other ports and Benzene clients would then see them only as app-defined, which by default
  maps to 500 or 200 (§3, lines 186-191).

(a) was recommended for the two precondition statuses, and agreed. `not-modified` is open: it only
means something on HTTP, so it could be an HTTP-binding rule instead of a vocabulary entry.

**Acceptance tests.**
- `PUT` with a matching `If-Match` gives 200 with the new `ETag`. A stale one gives 412 with a problem
  document. A missing one, when required, gives 428.
- `GET` with a matching `If-None-Match` gives 304 with no body and the `ETag` header.
- Conformance fixtures for the new statuses pass in .NET.

**Spec change.** **Yes**, under option (a): wire-contracts §3, §3.1 and §4.1, plus fixtures. Every
port re-vendors.

### FR-7 — Rate and concurrency limits across Lambda instances

**Today.** Partial: limits are per instance.
- `src/Benzene.RateLimiting/CLAUDE.md:4`: "Best-effort, **per-instance** rate limiting".
  `docs/rate-limiting.md:9-16` says authoritative limiting belongs at the gateway.
- Any `System.Threading.RateLimiting` limiter plugs in through `UseRateLimiting(RateLimiter)`
  (`Extensions.cs:27`) or `UsePartitionedRateLimiting` (`:72`), including `ConcurrencyLimiter`
  (tested in `test/Benzene.Core.Test/Plugins/RateLimiting/RateLimitingPipelineTest.cs:142`). There
  is no `UseConcurrencyLimiting` shortcut.
- Rejection gives `too-many-requests` and a best-effort `Retry-After` (`RateLimitingMiddleware.cs:167-187`).
- There is no shared store. On Lambda each concurrent instance has its own limiter, so the effective
  limit is the configured limit times the instance count.

**Why it is needed.** Today's service caps concurrent work per tenant across all instances, for
example to protect a downstream system.

**Assess first.** Check whether AWS covers it without code:
- Lambda reserved concurrency caps the function as a whole.
- API Gateway usage plans and throttling cap request rate per API key.

If the need is per tenant or per key across instances, it is code.

**Requirements.**
1. A distributed limiter with a pluggable store, and a DynamoDB implementation. Reuse the shape and
   conventions of `src/Benzene.Idempotency.DynamoDb` (a table, conditional writes, TTL).
2. Both kinds: a rate limit (fixed window or token bucket per partition key) and a concurrency limit
   (leases held for the duration of the request, released after `next()`). Leases expire by TTL so a
   crashed instance can't hold one forever.
3. The partition key comes from the context: caller (FR-4), tenant claim, API key or route.
4. Same rejection behaviour as today: `too-many-requests` and `Retry-After`.
5. A store failure has a configurable policy, fail open or fail closed. Default to fail open, logged.
6. The extra latency per request is measured and documented.

**Suggested API.**
```csharp
app.UseDistributedConcurrencyLimit(o =>
{
    o.PermitLimit = 10;
    o.PartitionBy = ctx => ctx.Caller()?.FindFirst("tenant")?.Value ?? "anonymous";
    o.LeaseTtl = TimeSpan.FromSeconds(30);
}).WithDynamoDbStore("benzene-limits");
```

**Acceptance tests.**
- Against DynamoDB Local or a fake store: two "instances" sharing a store enforce one limit.
- A lease is released on success, on failure and on exception.
- An expired lease frees its slot.
- The fail-open and fail-closed policies behave as configured.

**Spec change.** No.

### FR-8 — One event per request to a pluggable sink

**Today.** Partial.
- `UseMeshTrace` (`src/Benzene.Mesh.Wire/Extensions.cs:36-53`) emits one `MeshTraceEvent` per
  invocation: trace and span ids, service, topic, version, status, exception type, duration, start
  time and correlation id. It goes to `IMeshTraceExporter.Export`. The exporter is lossy by design,
  and its only implementation POSTs to the mesh collector.
- `Benzene.CloudService` wires it only on the envelope pipeline (`src/Benzene.CloudService/Extensions.cs:212`).
  The HTTP-native routes (`:220-224`) get no trace.
- `UseLogResult` (`src/Benzene.Core.Middleware/LoggerExtensions.cs:28-72`) writes a log line, not an
  event.
- There is no audit package and no SQS sink.

**Why it is needed.** Today's service writes one durable event per request (audit and analytics),
including requests rejected before a handler runs: unauthenticated, forbidden, invalid or throttled.

**Requirements.**
1. A request-event middleware, placed outermost, that emits exactly one event per request on every
   host, the HTTP-native routes included, whatever the outcome.
2. The event carries:
   - method, route template and path, status and HTTP status, duration and timestamps;
   - correlation and trace ids;
   - the caller (FR-4) where known;
   - the rejection stage (authentication, authorization, validation, rate limit, not found) when no
     handler ran.
   Bodies and headers are excluded by default, with an explicit allow-list.
3. A pluggable sink interface with an SQS implementation (batched, async, with delivery failure
   handled by policy). Unlike mesh tracing, this sink is not lossy by default.
4. Emitting the event never changes the response, and a sink failure never fails the request unless
   configured to.
5. Decide whether this is a new package or a durable sink for `MeshTraceEvent`.
   - Recommend a separate request-event type: the mesh event's shape is pinned by the spec
     (`docs/specification/mesh.md`) and is deliberately small.
   - Also extend `UseMeshTrace` to the HTTP-native routes, which is a gap on its own.

**Acceptance tests.**
- One event each for: success, 401, 403, `validation-error`, 429, `not-found` route, and unhandled
  exception, with the right rejection stage.
- A sink that throws doesn't change the response.
- SQS batching flushes within the Lambda invocation.

**Spec change.** Not for a port-local request event. Yes if `MeshTraceEvent` gains fields or the mesh
trace's coverage rule changes (`docs/specification/mesh.md`).

### FR-9 — Keep the current error body format

**Today. This mostly exists; the original request overstated the gap.**
- Failures are RFC 9457 problem documents built by `DefaultResponsePayloadMapper`
  (`src/Benzene.Core.MessageHandlers/Response/DefaultResponsePayloadMapper.cs:41`).
- The mapper is replaceable through `IResponsePayloadMapper<TContext>`
  (`src/Benzene.Abstractions.MessageHandlers/Response/IResponsePayloadMapper.cs:11`), registered with
  `TryAdd`, so a user registration wins (`src/Benzene.Core.MessageHandlers/DI/Extensions.cs:165`).
- A handler can also return its own document (`BenzeneResult.Problem<T>`, `BenzeneResult.cs:124`).

**What is actually missing.**
1. **Early rejections get no body.** `DefaultResponsePayloadMapper.cs:34` returns `null` when no
   handler was resolved, so an unmatched route, and possibly a middleware rejection before routing,
   has no error body in either format. Assess whether that breaks the spec's failure-payload rule
   (`docs/specification/wire-contracts.md` §1.3) and fix it whichever format is in use.
2. **No documented recipe.** Add a cookbook for replacing the error body, with a sample mapper that
   writes the adopter's existing format.
3. **Conformance consequence.** The spec says a failure payload is a valid RFC 9457 problem document
   (wire-contracts §1.3, line 58). A service that replaces it doesn't conform to that rule, and Benzene
   clients and the mesh can't read its `errors`. The cookbook must say so. Recommend scoping the
   override to the HTTP-native routes, leaving the envelope endpoint conforming.
4. Optionally, a smaller hook than the whole payload mapper, an `IProblemFormatter` that receives the
   built `ProblemDetails`, so an override doesn't re-implement success-payload mapping.

**Acceptance tests.**
- A registered custom mapper produces the custom body for a validation error and a 404 on v1, v2 and
  ASP.NET.
- An unmatched route produces a body: a problem document by default, the custom one when overridden.
- The envelope endpoint still returns a problem document when the override is scoped to HTTP routes.

**Spec change.** No. Whether an early rejection must carry a body is a reading of §1.3 to settle
during assessment.

### FR-10 — Lambda SnapStart

**Today.**
- There is no SnapStart code: searching for `SnapStart`, `SnapshotRestore`, `RegisterBeforeSnapshot`
  and `RegisterAfterRestore` finds nothing.
- What exists is warm-up in the init phase: `serviceResolverFactory.WarmUp()` in
  `src/Benzene.Aws.Lambda.Core/AwsLambdaHost.cs:55`, opt-in through `AddBenzeneWarmUp()` and
  `IWarmUpTask`. It never dispatches a message.
- `examples/AwsMesh/README.md:818` says SnapStart is unavailable on the `provided.al2023` custom
  runtime. That is true, but SnapStart does apply on the managed .NET runtime.
- `Benzene.Aws.Lambda.Core.csproj:13` pins `Amazon.Lambda.Core` 2.2.0.

**Why it is needed.** The current service uses SnapStart on the managed .NET runtime to keep cold
starts down.

**Requirements.**
1. Bump `Amazon.Lambda.Core` to a version with the `SnapshotRestore` API.
2. **Before snapshot:** run the existing warm-up, plus optional priming that sends a few synthetic
   requests through the real pipeline. Use configured sample requests per route, so JSON serializers,
   routing tables and DI graphs are built before the snapshot.
   - Priming must not reach real downstream systems or emit audit events (FR-8), mesh traces or logs
     as if it were real traffic. Mark the context as priming so middleware can skip side effects.
3. **After restore:** an `IAfterRestoreTask` hook to refresh what must not be shared between restored
   copies. That covers random seeds, cached credentials and tokens, connection pools, and
   per-instance ids such as the mesh instance id and the rate-limiter partitions.
4. Works with `Benzene.Aws.Lambda.Hosting` and the classic handler entry point. Doesn't change
   behaviour when SnapStart is off.
5. Document which Benzene components hold per-instance state that must refresh after restore.

**Suggested API.**
```csharp
services.AddBenzeneSnapStart(o =>
{
    o.PrimeWith(new ApiGatewayProxyRequest { HttpMethod = "GET", Path = "/tasks/health" });
});
services.AddAfterRestoreTask<RefreshCredentials>();
```

**Acceptance tests.**
- Unit level: the before-snapshot callback runs warm-up and priming, and priming produces no
  FR-8/mesh events.
- The after-restore tasks run.
- An example deployed with SnapStart on the managed runtime shows the restore hooks firing (a manual
  or CI smoke test).

**Spec change.** No.

---

## P2: lower priority

### FR-11 — Strict header versioning

**Today.** `src/Benzene.Core.MessageHandlers/VersionSelector.cs`:
- A missing version falls back to the default handler (lines 55-57, 81-98).
- An unknown version is `not-found` (line 23).
- No option rejects a missing version. The selector is replaceable (`IVersionSelector`, `TryAdd`,
  `DI/Extensions.cs:137`), and there is an opt-in `HighestAvailableVersionSelector`.

**Request.** An option to require the version header. A missing version would be `bad-request`, or
`validation-error` naming the header. An unknown version should be `bad-request` listing the
supported versions, not `not-found`, which reads like a missing route. Applies per topic or
pipeline-wide.

**Spec change.** Possibly. If an unknown version answers something other than `not-found`, check
`docs/specification/versioning.md` and `service-versions.md` first.

### FR-12 — Configurable JSON options

**Today.**
- `src/Benzene.Core.MessageHandlers/Serialization/JsonSerializer.cs` has fixed defaults (lines
  58-68: camelCase, case-insensitive, relaxed escaping, named float literals). It accepts custom
  options only through its constructor (line 75), by registering your own instance.
- There is no `Configure` API and no string-enum converter by default.
- Newtonsoft settings are hard-coded (`src/Benzene.NewtonsoftJson/JsonSerializer.cs:29,62`).
- The envelope always uses the default serialization (`src/Benzene.Http/CLAUDE.md`).

**Request.** `services.ConfigureBenzeneJson(o => …)` that adjusts the defaults (naming policy, enum
converter, ignore-null, custom converters) for handler bodies, and the same for Newtonsoft.
- Document that the envelope keeps the wire format the spec pins, whatever is configured.
- Acceptance: enums serialize as strings when configured; nulls are omitted when configured; the
  envelope fields are unchanged.

### FR-13 — OpenAPI for all of the above

**Today.** `src/Benzene.Schema.OpenApi/OpenApi/OpenApiDocumentBuilder.cs`:
- Path parameters only, typed as string (lines 125-137).
- The success response is always 200 (lines 144-154).
- Fixed error responses 400/401/403/404/422/500/503 (lines 241-251).
- No security schemes or scopes, and no response headers.

**Request.**
- Query parameters, including arrays (FR-1).
- The real success status: 201 for create, 204 where there is no body.
- Response headers (FR-3: `Location`, `ETag`).
- Conditional statuses (FR-6) and 409/429.
- Security schemes with per-operation scopes (FR-5).
- The base path as the server URL (FR-2).

Do each piece alongside the FR it describes, or as one pass at the end.

### FR-14 — Docs fixes

**Done 2026-11-19, benzene-dotnet `be33941`:**
- items 1 and 2 are fixed;
- item 3 is done for today's behaviour: `docs/rate-limiting.md` now names the AWS controls that cap
  across instances. Update it again when FR-7 ships;
- item 4 is a new "API Gateway" section in `docs/getting-started-aws.md`, which also lists what isn't
  built yet. Remove each line there as its FR ships.

Warm-up (`AddBenzeneWarmUp`, `IWarmUpTask`) is now in the cold-start cookbook, which no doc covered
before.

The items as raised:

1. `docs/getting-started-aws.md:591-592` says `[HttpEndpoint]` must match "exactly, including case".
   Route matching ignores case:
   - `src/Benzene.Http/Routing/CompiledRoutePath.cs:184,193-194` compares with `OrdinalIgnoreCase`.
   - `RouteFinder.cs:295,300` lower-cases the method.
   - `src/Benzene.Http/CLAUDE.md:172` and `work/outstanding-bugs.md` #91 say the same.
   Fix the getting-started line; parameter values keep their case.
2. `work/1.0-release-plan.md` item 4.6 says repeated query keys are "last-value-wins". The code is
   first-value-wins (FR-1). Correct it, or supersede it with FR-1.
3. The rate-limiting docs should link to FR-7's outcome: what is per instance, and what to use across
   instances.
4. Add the "what already exists" table above to the .NET docs. An adopter reviewing the docs did not
   find most of it.

---

## Build order

The order follows the dependencies: identity before authorization, headers before ETags, and spec
changes early, since they gate the ports.

1. **FR-14** (docs fixes): no dependencies, and it removes wrong guidance now.
2. **FR-1** (repeated query keys) and **FR-3** (response headers): small and independent.
3. **FR-2** (base path and request URL), which `Location` links need.
4. **FR-4** (authorizer identity), then **FR-5** (per-endpoint authorization), which needs a caller
   to check.
5. **FR-6**: settle the spec decision as soon as assessment starts, because a spec release gates
   it. Implement it after FR-3.
6. **FR-9**: the early-rejection body and the cookbook. Small, and FR-8 tests reuse the same
   rejection cases.
7. **FR-8**, after FR-4 so events carry the caller.
8. **FR-7**, after FR-4 so limits can partition by caller.
9. **FR-10**, after FR-8 so priming can be shown not to emit events.
10. **FR-11**, **FR-12**, **FR-13** (P2). FR-13 can also be done piece by piece alongside each item.

## Definition of done for the whole list

A sample service in benzene-dotnet, `examples/Aws/Benzene.Examples.Aws.Tasks` (named like its siblings), that is a small Tasks CRUD API on API
Gateway HTTP API (v2) and uses every item:
- mounted under `/tasks` (FR-2);
- caller from a JWT authorizer (FR-4);
- `tasks.read` and `tasks.write` per endpoint (FR-5);
- list filters with repeated keys (FR-1);
- `201` with `Location` (FR-3);
- `ETag` and `If-Match` on update, and 304 on read (FR-6);
- a per-tenant concurrency limit on DynamoDB (FR-7);
- one audit event per request to SQS (FR-8);
- a custom error body on its HTTP routes (FR-9);
- SnapStart on the managed runtime (FR-10);
- strict versioning (FR-11) and string enums (FR-12);
- an OpenAPI document that describes all of it (FR-13).

It has a test project with tests for each feature through the test helpers. It deploys and passes a
smoke test in CI, as `Benzene.Examples.Aws` does through `.github/workflows/deploy-aws-example.yml`.
