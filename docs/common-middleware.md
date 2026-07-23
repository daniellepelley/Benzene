# Common Middleware

This page catalogs Benzene's general-purpose, transport-agnostic `.Use*()` middleware — the
building blocks you'll reach for on almost any pipeline, regardless of whether it's running on
AWS Lambda, Azure Functions, ASP.NET Core, or a self-hosted worker. Transport-specific middleware
(`UseApiGateway`, `UseSqs`, `UseSns`, `UseKafka`, `UseEventHub`, ...) is covered in the
platform getting-started guides instead.

Every entry below has been verified against the current source in `src/`. Where a deeper reference
page already exists (health checks, validation, monitoring), this page gives the essentials and
links out for the full story.

## Table of contents

- [UseTimer](#usetimer)
- [UseBenzeneEnrichment](#usebenzeneenrichment)
- [UseBenzeneMetrics](#usebenzenemetrics)
- [UseW3CTraceContext](#usew3ctracecontext)
- [UseBenzeneInvocation](#usebenzeneinvocation)
- [UseHealthCheck](#usehealthcheck)
- [UseSpec](#usespec)
- [UseMessageHandlers](#usemessagehandlers)
- [UsePresetTopic](#usepresettopic)
- [UseFluentValidation](#usefluentvalidation)
- [UseJsonSchema](#usejsonschema)
- [UseLogResult / UseLogContext](#uselogresult--uselogcontext)
- [UseExceptionHandler](#useexceptionhandler)
- [UseRetry](#useretry)
- [UseCors](#usecors)
- [UseOAuth2Bearer](#useoauth2bearer)
- [UseBasicAuth](#usebasicauth)
- [RequireScope](#requirescope)
- [UseXml](#usexml)

---

## UseTimer

**Package:** `Benzene.Diagnostics` (`Benzene.Diagnostics.Timers.Extensions`)

Opens a named [`Activity`](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing)
span around the rest of the pipeline. Every middleware already gets its own `Activity` automatically
via `AddDiagnostics()` — `UseTimer` is for naming a specific stage explicitly so it stands out in an
exported trace. Internally it resolves the registered `IProcessTimerFactory` (the
`AddDiagnostics()`-registered default, `ActivityProcessTimerFactory`, opens a real `Activity`); if
none is registered it's a no-op wrapper around `next()`.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseTimer<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, string timerName)

// overload: report elapsed milliseconds yourself instead of via IProcessTimerFactory
public static IMiddlewarePipelineBuilder<TContext> UseTimer<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, Action<TContext, long> onTimer)
```

```csharp
app.UseTimer("benzene-message-application");

// or handle the elapsed time yourself:
app.UseTimer((context, elapsedMs) => myMetrics.Record(elapsedMs));
```

See also: [Monitoring — Named timers](monitoring.md#named-timers).

---

## UseBenzeneEnrichment

**Package:** `Benzene.Diagnostics` (`Benzene.Diagnostics.EnrichmentExtensions`)

One portable, explicit-opt-in call that attaches `invocationId`, `traceId`, `spanId`, `topic`,
`transport`, and `handler` to the logging scope (via `ILogger.BeginScope`) for the duration of the
request, and tags the current `Activity` with `benzene.invocationId` — all in a single method that
works the same way on AWS Lambda, Azure Functions, and ASP.NET Core. Each key is resolved
independently and simply omitted if its backing service isn't registered for that pipeline (for
example, `invocationId` requires `UseBenzeneInvocation()` to have been called on this pipeline or
an outer one; it's omitted inside a per-message SQS/SNS/Kafka sub-pipeline, since
`IBenzeneInvocation` doesn't flow into those nested DI scopes today).

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseBenzeneEnrichment<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app)
```

```csharp
app.UseBenzeneEnrichment();
```

**When to use it:** on every platform. It replaced the AWS-only
`WithRequestId()`/`WithApplication()` log-context extensions, which have been removed.

---

## UseBenzeneMetrics

**Package:** `Benzene.Diagnostics` (`Benzene.Diagnostics.MetricsExtensions`)

Records `benzene.messages.processed` (a count) and `benzene.message.duration` (in milliseconds) for
the wrapped pipeline stage, tagged by `topic`, `transport`, and `result` (`success`/`failure`, read
from `IHasMessageResult` when the context implements it). Unlike the automatic per-middleware
`Activity` spans from `AddDiagnostics()`, this is once-per-message granularity and must be added
explicitly around the stage you want measured.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseBenzeneMetrics<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app)
```

```csharp
app.UseBenzeneMetrics();
```

Wire `Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation()` against an OTel
`MeterProviderBuilder` to actually export these to a real backend — see
[Monitoring — OpenTelemetry](monitoring.md#opentelemetry).

---

## UseW3CTraceContext

**Package:** `Benzene.Diagnostics` (`Benzene.Diagnostics.W3CTraceContextExtensions`)

Reads the `traceparent`/`tracestate` headers (matched case-insensitively) and starts the pipeline's
root `Activity` with the parsed remote context as its parent, so distributed traces continue across
services instead of each hop starting a new, disconnected trace. Falls back to a normal, parentless
root span when the headers are missing or fail to parse — it's always safe to add.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseW3CTraceContext<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app)
```

```csharp
app.UseW3CTraceContext();
```

**Add this as the FIRST middleware in the pipeline** — everything added after it inherits this
`Activity` as the ambient `Activity.Current` parent, so every automatically-wrapped middleware span
from `AddDiagnostics()` correctly nests under the remote trace.

Only wired for HTTP-based transports today (ASP.NET Core, Azure Functions' ASP.NET-style trigger,
API Gateway) — SQS/SNS/Kafka/Event Hub inbound extraction is not yet implemented. To propagate a
trace to a downstream Benzene service, see `.UseW3CTraceContext()` on an outbound route, described in
[Monitoring — Distributed Tracing](monitoring.md#distributed-tracing).

---

## UseBenzeneInvocation

**Package:** `Benzene.Core.Middleware` (`Benzene.Core.Middleware.BenzeneInvocationExtensions`)

Builds and exposes an `IBenzeneInvocation` for the duration of the request so it can be injected
wherever needed, and so [`UseBenzeneEnrichment()`](#usebenzeneenrichment) can populate
`invocationId`. You don't normally call the core overload directly — hosting platforms expose their
own zero-argument overload that supplies the factory (e.g. `Benzene.Aws.Lambda.Core`'s or
`Benzene.AspNet.Core`'s `UseBenzeneInvocation()`).

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseBenzeneInvocation<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app,
    Func<IServiceResolver, TContext, IBenzeneInvocation> factory)
```

```csharp
// on AWS Lambda / ASP.NET Core, just call the platform's zero-arg overload:
app.UseBenzeneInvocation();
```

Requesting `IBenzeneInvocation` from DI before this middleware has run for the current invocation
throws a `BenzeneException` — it's only populated for the duration of the request this middleware
wraps.

---

## UseHealthCheck

**Package:** `Benzene.HealthChecks` (`Benzene.HealthChecks.Extensions`)

Lets health checks be triggered by sending a message on a given topic. By default, `"healthcheck"`
always matches too (alongside whatever topic you register), so a single service's own health check
is always reachable at the default topic while you're free to also expose it under something like
`"<service-name>:healthcheck"` for cross-service calls.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, string topic, params IHealthCheck[] healthChecks)

public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, string topic, Action<IHealthCheckBuilder> action)

public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, string topic, IHealthCheckBuilder builder)
```

```csharp
app.UseHealthCheck("healthcheck", x => x
    .AddHealthCheck<MyDatabaseHealthCheck>()
    .AddHealthCheck(resolver => resolver.GetService<MyQueueHealthCheck>()));

// or pass already-constructed instances directly:
app.UseHealthCheck("healthcheck", new MyDatabaseHealthCheck(), new MyQueueHealthCheck());
```

`IHealthCheckBuilder` only supports `AddHealthCheck<THealthCheck>()` (resolved from DI) and
`AddHealthCheck(Func<IServiceResolver, IHealthCheck>)` directly — plus the `AddHealthChecks(params IHealthCheck[])`
and `AddHealthCheckFactory(IHealthCheckFactory)` extension helpers built on top of those.

On HTTP-based transports (ASP.NET Core, API Gateway), an additional overload lets you match on HTTP
method and path instead of (or as well as) a topic.

See also: [Health Checks](health-checks.md) for a full worked example.

---

## UseSpec

**Package:** `Benzene.Schema.OpenApi` (`Benzene.Schema.OpenApi.Extensions`)

Registers a handler that serves OpenAPI/AsyncAPI (and Benzene's own code-gen) schemas for the
service under the given topic (`"spec"` by default). This is essential if you want to use the
Benzene command-line code-gen tools against this service.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseSpec<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app)                       // uses topic "spec"

public static IMiddlewarePipelineBuilder<TContext> UseSpec<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, string topic)
```

```csharp
app.UseSpec("spec");
```

See also: [Spec](spec.md) for the request/response format.

---

## UseMessageHandlers

**Package:** `Benzene.Core.MessageHandlers` (`Benzene.Core.MessageHandlers.MiddlewarePipelineExtensions`)

The middleware that routes the raw message to a message handler, by pulling out the topic and
deserializing the payload. You can add additional middleware to the message router itself — most
commonly validation — via the `router =>` callback.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app)
    // scans AppDomain.CurrentDomain.GetAssemblies()

public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, params Assembly[] assemblies)

public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, params Type[] types)

public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, Action<MessageRouterBuilder> router)

public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, Assembly assembly, Action<MessageRouterBuilder> router)

public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, Assembly[] assemblies, Action<MessageRouterBuilder> router)

public static IMiddlewarePipelineBuilder<TContext> UseMessageHandlers<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, Type[] types, Action<MessageRouterBuilder> router)
```

```csharp
app.UseMessageHandlers(router => router
    .UseFluentValidation());
```

See also: [Message Handlers](message-handlers.md).

---

## UsePresetTopic

**Package:** `Benzene.Core.MessageHandlers` (`Benzene.Core.MessageHandlers.MiddlewarePipelineExtensions`)

Routes **every** message on this one pipeline to a fixed topic, regardless of what (if anything)
the transport message itself carries. For a queue or subscription whose producer isn't a Benzene
client and never sets the usual `topic` message attribute/property (a raw SQS send, a Service Bus
topic fed by another system, etc.) — call it before `UseMessageHandlers()`, on that specific
pipeline only. A queue whose producer does send a proper topic just omits it and is unaffected.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UsePresetTopic<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, string topicId, string version = "")
```

```csharp
// A queue whose producer never sets a topic attribute at all:
app.UseSqs(sqs => sqs
    .UsePresetTopic("orders.created")
    .UseMessageHandlers());

// A second queue in the same app whose producer DOES send a proper topic just omits it:
app.UseSqs(otherQueueConfig, sqs => sqs
    .UseMessageHandlers());
```

Once set, the preset topic always wins for that pipeline's messages — even if a message happens to
carry a stray `topic` attribute — so there's no separate "fallback vs. override" mode to configure.

Implementation note for anyone extending Benzene with a new transport: the preset topic is carried
in a small scoped (per-message) DI service, not a property on the transport context — a context
type stays a pure description of the transport message, per this repo's context-purity convention.
`UsePresetTopic<TContext>` has no constraints on `TContext` at all, so any transport gets this for
free by registering `PresetTopicMessageTopicGetter<TContext>` (decorating its own real topic
getter) as the default `IMessageTopicGetter<TContext>`, alongside a
`services.TryAddScoped<PresetTopicHolder>()` call — no context changes required. `Benzene.Aws.Lambda.Sqs`,
`Benzene.Aws.Sqs`, and `Benzene.Azure.Function.ServiceBus` are the reference implementations.

---

## UseFluentValidation

**Package:** `Benzene.FluentValidation` (`Benzene.FluentValidation.DependencyExtensions`)

Nests inside `UseMessageHandlers(router => ...)`. Attempts to find a FluentValidation `IValidator<T>`
for the request type; if a validator is found and validation fails, it short-circuits with a
validation-failure result before the request ever reaches the message handler. If no validator is
registered for the type, the request passes straight through.

```csharp
public static IMessageRouterBuilder UseFluentValidation(
    this IMessageRouterBuilder builder, params Assembly[] assemblies)

public static IMessageRouterBuilder UseFluentValidation(
    this IMessageRouterBuilder builder, Type[] types)
```

```csharp
app.UseMessageHandlers(router => router
    .UseFluentValidation());
```

See also: [Fluent Validation](fluent-validation.md).

---

## UseJsonSchema

**Package:** `Benzene.JsonSchema` (`Benzene.JsonSchema.Extensions`)

Generates a JSON Schema (draft 2020-12, via `JsonSchema.Net.Generation`) from the request type of
the handler registered for the current topic, and validates the incoming payload against it before
the handler runs. Uses camelCase property names to match the default serializer. If the topic has
no registered handler, validation is skipped. Generated schemas are cached per request type.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseJsonSchema<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app)
    where TContext : class
```

```csharp
app.UseJsonSchema();
```

Register a custom `IJsonSchemaProvider<TContext>` if you need schema generation behavior other than
the default.

---

## UseLogResult / UseLogContext

**Package:** `Benzene.Core.Middleware` (`Benzene.Core.Middleware.LoggerExtensions`)

Both attach properties to the logging scope (via `ILogger.BeginScope`) for the duration of the
request, configured through the same `ILogContextBuilder<TContext>` fluent builder:

- **`UseLogResult`** additionally measures processing time and emits a single structured
  `"BenzeneResult"` log line per execution, with request-scope, response-scope, and `processTime`
  properties all attached.
- **`UseLogContext`** just enriches the scope for the request — it doesn't log anything itself.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseLogResult<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, Action<ILogContextBuilder<TContext>> action)

public static IMiddlewarePipelineBuilder<TContext> UseLogContext<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, Action<ILogContextBuilder<TContext>> action)
```

```csharp
app.UseLogResult(x => x
    .WithCorrelationId()
    .WithTopic()
    .WithTransport());
```

### Log-context builder extensions (`.With*()`)

These configure what `UseLogResult`/`UseLogContext` attach to the scope:

| Extension | Package | Adds |
| --- | --- | --- |
| `WithCorrelationId()` | `Benzene.Diagnostics` (`Correlation.Extensions`) | the current `ICorrelationId` value, keyed `correlationId` |
| `WithTopic()` | `Benzene.Core.MessageHandlers` | the current message's topic, keyed `topic` (`"<missing>"` if unresolvable) |
| `WithTransport()` | `Benzene.Core.MessageHandlers` | the current `ICurrentTransport.Name`, keyed `transport` |
| `WithHeaders(params string[] headers)` | `Benzene.Core.MessageHandlers` | the named request headers, each keyed by its own header name |

> The AWS-only **`WithRequestId()` and `WithApplication()`** log-context extensions
> (`Benzene.Aws.Lambda.Core.LogContextBuilderExtensions`) have been **removed** — use
> [`UseBenzeneEnrichment()`](#usebenzeneenrichment), which attaches the equivalent `invocationId`
> key on every platform, not just AWS Lambda. (The still-current `WithApplication()` in
> `Benzene.Core.MessageHandlers` is `IApplicationInfo`-based and transport-agnostic, and is a
> different method — it remains.)

```csharp
app.UseBenzeneEnrichment();
```

See also: [Monitoring — Logging](monitoring.md#logging).

---

## UseExceptionHandler

**Package:** `Benzene.Core.Middleware` (`Benzene.Core.Middleware.Extensions`)

Adds centralized exception handling around the rest of the pipeline: any exception thrown by
downstream middleware or the handler is caught and passed to `onException`, letting you log it,
transform it into an error response on the context, or otherwise handle it in a context-aware way.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseExceptionHandler<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, Action<TContext, Exception> onException)
```

```csharp
app.UseExceptionHandler((context, exception) =>
{
    // e.g. log it and/or map it onto the context's result
});
```

---

## UseRetry

**Package:** `Benzene.Resilience` (`Benzene.Resilience.Extensions`)

Wraps the rest of the pipeline in a retry loop with exponential backoff. Retries on any exception
except `OperationCanceledException` by default, and does not retry a *successful* result unless you
supply `shouldRetryContext`. This is a small, hand-rolled retry loop — it does not depend on Polly.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseRetry<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app,
    int numberOfRetries = 3,
    TimeSpan? initialDelay = null,      // defaults to 200ms
    double backoffFactor = 2.0,
    Func<Exception, bool>? shouldRetry = null,          // defaults to "retry unless OperationCanceledException"
    Func<TContext, bool>? shouldRetryContext = null,     // defaults to "never retry a completed result"
    Func<TimeSpan, Task>? delay = null)                  // defaults to Task.Delay
```

```csharp
app.UseRetry(numberOfRetries: 5, initialDelay: TimeSpan.FromMilliseconds(100));
```

---

## UseCors

**Package:** `Benzene.Http` (`Benzene.Http.Cors.Extensions`)

Adds CORS handling for HTTP-based contexts: validates the `Origin` header against your configured
allowed domains/headers and automatically answers preflight `OPTIONS` requests. Add it early in the
pipeline so CORS headers get applied to every response.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseCors<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, CorsSettings corsSettings)
    where TContext : IHttpContext
```

```csharp
app.UseCors(new CorsSettings
{
    AllowedDomains = new[] { "https://example.com" },
    AllowedHeaders = new[] { "Content-Type", "Authorization" },
    ExposedHeaders = new[] { "X-Total-Count" },  // adds Access-Control-Expose-Headers
    AllowCredentials = true,   // adds Access-Control-Allow-Credentials: true
    MaxAgeSeconds = 600,       // adds Access-Control-Max-Age on preflight responses
});
```

Applies to any `TContext : IHttpContext` — ASP.NET Core, API Gateway, etc.
Behavior tracks the CORS specification the same way `Microsoft.AspNetCore.Cors` does:

- **Origin matching is exact (scheme + host + port) when you specify a full URL.**
  `AllowedDomains = ["https://example.com"]` matches only `https://example.com` — not
  `http://example.com` (different scheme) and not `https://example.com:8080` (different port).
  A bare hostname entry (`"example.com"`, no scheme) is more permissive and matches that host
  under any scheme/port, as a documented shorthand. `"*"` allows any origin; the middleware always
  echoes back the actual `Origin` value rather than a literal `"*"`, so it's safe to combine with
  `AllowCredentials = true` (the CORS spec forbids a literal `"*"` for Allow-Origin/Allow-Headers
  when credentials are allowed, but permits echoing the real value).
- **`AllowedHeaders` accepts `"*"`** to allow any header (equivalent to `AllowAnyHeader()`); like
  origins, the middleware echoes back the requested headers instead of a literal `"*"`. With an
  explicit list, a preflight asking for a header outside that list (via
  `Access-Control-Request-Headers`) fails the CORS check entirely — no CORS headers are returned,
  and the browser blocks the real request, matching `CorsService`'s behavior in ASP.NET Core.
- **`Vary: Origin`** is set on every response the middleware processes, so caches/CDNs in front of
  the API don't serve one origin's CORS-tailored response to another.
- **`Access-Control-Expose-Headers`** (via `ExposedHeaders`) is sent on actual (non-preflight)
  responses only, since it's meaningless on a preflight response.

---

## UseOAuth2Bearer

**Package:** `Benzene.Auth.OAuth2` (`Benzene.Auth.OAuth2.Extensions`)

OAuth2 bearer token (JWT) validation for services that have no security-terminating gateway (API
Gateway, a load balancer with auth, etc.) in front of them. Reads `Authorization: Bearer <token>`,
validates it against a JWKS endpoint (via OIDC discovery or a bare JWKS URI), and either
short-circuits with `unauthorized` or sets the authenticated caller for later pipeline steps. See
the [Authentication Patterns cookbook](cookbooks/auth-patterns.md) for a full worked example.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseOAuth2Bearer<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, OAuth2BearerOptions options)
    where TContext : IHttpContext
```

```csharp
app.UseOAuth2Bearer(new OAuth2BearerOptions
{
    Authority = "https://your-tenant.auth0.com/.well-known/openid-configuration",
    ValidIssuers = new[] { "https://your-tenant.auth0.com/" },
    ValidAudiences = new[] { "your-api-identifier" },
    ValidAlgorithms = new[] { "RS256" },
});
```

Applies to any `TContext : IHttpContext`. `Authority`/`JwksUri`, `ValidIssuers`, `ValidAudiences`,
and `ValidAlgorithms` are all required — an empty allowlist would silently under-validate every
token, so `UseOAuth2Bearer` throws at wire-up time rather than accepting tokens from any
issuer/audience/algorithm. `ValidAlgorithms` in particular has no permissive default: a validator
that trusted whatever `alg` a token claimed would be open to algorithm-confusion attacks (RFC 8725
§3.1). A failed validation always returns a generic `unauthorized` detail — the real reason (bad
signature, expired, wrong issuer/audience) is never echoed back to the caller, only logged
server-side. On success, the validated claims are available to `RequireScope` (below) and to your
own handlers via `Benzene.Auth.Core.AuthenticationHolder`.

---

## UseBasicAuth

**Package:** `Benzene.Auth.Basic` (`Benzene.Auth.Basic.Extensions`)

RFC 7617 HTTP Basic authentication — the simplest option when you just need a username/password
gate (a single service account, an internal admin surface) rather than full OAuth2. Validates the
decoded credentials against your own `IBasicAuthCredentialValidator`; ships no default
implementation, so there's no hardcoded-credential footgun to accidentally deploy.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseBasicAuth<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app,
    IBasicAuthCredentialValidator validator, string realm = "Benzene")
    where TContext : IHttpContext
```

```csharp
public class ServiceAccountValidator : IBasicAuthCredentialValidator
{
    public Task<ClaimsPrincipal?> ValidateAsync(string username, string password)
    {
        var expected = Environment.GetEnvironmentVariable("SERVICE_ACCOUNT_PASSWORD");
        if (username != "service-account" || password != expected)
        {
            return Task.FromResult<ClaimsPrincipal?>(null);
        }
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) });
        return Task.FromResult<ClaimsPrincipal?>(new ClaimsPrincipal(identity));
    }
}

app.UseBasicAuth(new ServiceAccountValidator());
```

Applies to any `TContext : IHttpContext`. A missing/malformed header or a validator returning
`null` short-circuits with `unauthorized` and a `WWW-Authenticate: Basic realm="..."` challenge
header (per RFC 7617, so browsers/HTTP clients actually prompt for credentials). Credentials are
split on the *first* `:` only — a password containing `:` is preserved intact, not truncated.

---

## RequireScope

**Package:** `Benzene.Auth.OAuth2` (`Benzene.Auth.OAuth2.Extensions`)

Basic scope-based authorization: requires the caller authenticated by `UseOAuth2Bearer`, earlier in
the pipeline, to hold at least one of the given scopes. Reads both the `scope` claim (RFC 8693,
space-delimited) and the `scp` claim (Azure AD's convention — a space-delimited string or a JSON
array, depending on issuer). Scope checks are the simplest of several authorization primitives
Benzene ships: `Benzene.Auth.Core` (`Benzene.Auth.Core.AuthorizationExtensions`) also provides
`RequireRole`, `RequirePolicy` (name-, predicate-, and `AddAuthorizationPolicy`-based), and
resource-based `RequireAuthorization<TContext, TResource>`, all layered on the `ClaimsPrincipal` this
middleware exposes — see `docs/cookbooks/auth-patterns.md` §"Authorization: Roles, Policies &
Resource Checks".

```csharp
public static IMiddlewarePipelineBuilder<TContext> RequireScope<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, params string[] anyOfScopes)
    where TContext : IHttpContext
```

```csharp
app.UseOAuth2Bearer(oauth2Options)
   .RequireScope("orders:write");   // any one of the given scopes is sufficient
```

Applies to any `TContext : IHttpContext`, chained after `UseOAuth2Bearer`. No authenticated caller
at all (no auth middleware ran, or it ran and failed) short-circuits with `unauthorized` — a
caller present but missing every requested scope short-circuits with `forbidden` instead. These
are deliberately distinct statuses (`unauthorized` = not authenticated, `forbidden` = authenticated
but not permitted); collapsing them would leave API consumers unable to tell the two apart from a
403 alone.

---

## UseXml

**Package:** `Benzene.Xml` (`Benzene.Xml.DependencyInjectionExtensions`)

Registers an XML `IMediaFormat<TContext>` (`XmlMediaFormat<TContext>`) alongside the default JSON
one, so the pipeline can serialize/deserialize XML payloads in addition to JSON — which format
applies to a given request/response is resolved per-message by `IMediaFormatNegotiator<TContext>`
(`content-type` for reads, `accept` for writes).

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseXml<TContext>(
    this IMiddlewarePipelineBuilder<TContext> source)
    where TContext : class
```

```csharp
app.UseXml();
```

---

## UseMessagePack

**Package:** `Benzene.MessagePack` (`Benzene.MessagePack.DependencyInjectionExtensions`)

Registers a MessagePack `IMediaFormat<TContext>` (`MessagePackMediaFormat<TContext>`) alongside
the default JSON one (and XML, if `UseXml()` is also called) — same per-message negotiation as
`UseXml()`, via `content-type`/`accept: application/msgpack`. MessagePack is a genuinely binary
format, but every Benzene transport's body is a `string`; `MessagePackSerializer` Base64-armors the
msgpack bytes so it works unchanged through the existing string-based pipeline (see
`Benzene.MessagePack`'s `CLAUDE.md` for the full rationale) — a client sending/receiving MessagePack
must Base64-decode/encode the body accordingly.

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseMessagePack<TContext>(
    this IMiddlewarePipelineBuilder<TContext> source)
    where TContext : class
```

```csharp
app.UseMessagePack();
```

---

## A note on removed packages

`Benzene.Datadog`, `Benzene.Zipkin`, and `Benzene.Aws.XRay` have been removed. Their vendor-specific
timer backends are superseded by `Benzene.Diagnostics`'s `ActivityProcessTimerFactory`, which backs
`UseTimer(name)` with a real `System.Diagnostics.Activity` and works with any OpenTelemetry-compatible
backend via `Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation()` — see
[Monitoring — OpenTelemetry](monitoring.md#opentelemetry).
