# Privacy & Data Handling

This page describes what data Benzene's observability features (logging, tracing, health checks)
capture by default and where you, as the application developer, control whether personally
identifiable information (PII) or other sensitive data ends up in your logs/traces. **This is not
legal advice and does not constitute a GDPR compliance certification** — Benzene is a library, not a
data processor; whether your *application* is GDPR-compliant depends on what your handlers do with
data, what you configure Benzene to capture, and your own organization's data-handling practices.
This page exists to give you an accurate picture of Benzene's own behavior so you can make that
assessment.

## What Benzene captures automatically, with no configuration

- **Tracing (`AddDiagnostics()` / `Activity` spans):** every span is tagged with
  `benzene.transport` (e.g. `"http"`, `"sqs"`), `benzene.topic`, `benzene.version`, and
  `benzene.handler` (the .NET type name of the resolved handler) — see
  `src/Benzene.Diagnostics/ActivityMiddlewareDecorator.cs`. None of these carry request/response
  payload data, headers, or user-supplied values. `UseBenzeneEnrichment()` additionally tags
  `benzene.invocationId` (a generated identifier, not user data) — see
  `src/Benzene.Diagnostics/EnrichmentExtensions.cs`.
- **Metrics (`UseBenzeneMetrics()`):** counts and durations only (`benzene.messages.processed`,
  `benzene.message.duration`), dimensioned by transport/topic/status — no payload data.
- **W3C trace context propagation (`UseW3CTraceContext()`):** propagates only the standard
  `traceparent`/`tracestate` header values (trace ID, span ID, sampling flags) as defined by the
  [W3C Trace Context spec](https://www.w3.org/TR/trace-context/) — these are opaque identifiers, not
  application data, and the spec itself is designed not to carry PII.
- **Logging:** `UsingBenzene(...)` wires up `ILogger<T>` but emits no framework log lines unless you
  explicitly add `UseLogResult(...)`/`UseLogContext(...)` middleware (see below) or your own handler
  code calls `_logger.Log*(...)`. There is no default request/response body logging anywhere in the
  framework.

## Where you opt in to capturing more — and where the risk is

Everything below requires an explicit call in your own `StartUp`/pipeline configuration. None of it
happens unless you write it.

- **`WithHeaders(params string[] headers)`** (see [Common Middleware](common-middleware.md#uselogresult--uselogcontext))
  logs the *named* request headers verbatim, keyed by their own header name. This is the one built-in
  extension point where a careless call can leak something sensitive — e.g. `WithHeaders("Authorization")`
  would log a bearer token in plaintext. Only pass header names you've confirmed don't carry
  credentials or PII (correlation IDs, tenant IDs, and similar routing-only headers are the intended
  use case).
- **`UseLogResult(...)`'s "BenzeneResult" log line** logs only the literal message `"BenzeneResult"`
  plus whatever scope properties your `.With*()` configuration attaches (`correlationId`, `topic`,
  `transport`, and/or the headers above) — see `src/Benzene.Core.Middleware/LoggerExtensions.cs`. It
  does not log the request or response body itself under any configuration.
- **Your own handler code.** Any `_logger.LogInformation(...)` call you write inside a message
  handler can log whatever you pass it, including full request payloads if you're not careful.
  Benzene's structured logging (`ILogger`'s message-template overloads, e.g.
  `LogInformation("Processing order {OrderId}", orderId)`) is the safer pattern versus string
  interpolation, both for avoiding accidental PII capture (log the specific fields you mean to, not
  the whole object) and for avoiding log-injection-style issues from unsanitized values landing
  directly in the message text.
- **Custom `Activity` tags.** If you add your own `Activity.SetTag(...)` calls in handler code (e.g.
  via `Activity.Current`), the same care applies — tags you add are your responsibility, not
  Benzene's.
- **Health check diagnostics (`Benzene.HealthChecks.*`).** `IHealthCheckResult.Data` is a free-form
  dictionary each check populates itself. The built-in checks
  (`Benzene.HealthChecks.Http.HttpPingHealthCheck`, `Benzene.HealthChecks.EntityFramework`'s
  `DatabaseConnectionHealthCheck`/`DatabaseHealthCheck`) include the checked URL, connectivity
  status, and — on failure — the caught exception's `.Message`. Some ADO.NET/database driver
  exceptions can include connection details in their message text; if your health check topic is
  reachable by anyone other than trusted internal callers/monitoring systems, treat that as a
  potential information-disclosure surface and restrict who/what can invoke the health check topic
  accordingly (Benzene's health check middleware itself does not apply any authorization — that's
  the same responsibility you'd have for any other message-handler topic).

## Sampling and data retention

Tracing sampling (see [Sampling Strategies](sampling-strategies.md)) reduces *how much* trace data is
exported, which indirectly reduces exposure if your spans ever did carry sensitive tags — but it's
not a substitute for not capturing sensitive data in the first place, since sampled spans still
contain whatever was tagged on them. Data retention (how long your tracing backend/log aggregator
keeps data) is entirely a function of your backend's configuration, not something Benzene controls or
has an opinion on.

## Summary checklist

- [ ] Audit every `WithHeaders(...)` call for header names that might carry credentials or PII
- [ ] Review handler-level `_logger.Log*(...)` calls for accidental payload/PII logging
- [ ] If your health check topic is externally reachable, confirm it doesn't need authorization, or
      add it yourself (Benzene doesn't provide it)
- [ ] Configure your tracing backend's/log aggregator's own retention policy to match your
      organization's data-retention requirements — Benzene has no retention controls of its own
- [ ] If you have GDPR (or similar) obligations, have your own legal/compliance review cover what
      your *handlers* do with personal data, not just Benzene's framework-level behavior described here
