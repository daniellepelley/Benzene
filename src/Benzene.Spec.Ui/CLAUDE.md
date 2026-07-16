# Benzene.Spec.Ui

## What this package does
Serves a self-contained, Swagger-UI-style web viewer for the **Benzene message spec** — the
`benzene`-format spec produced by `Benzene.Schema.OpenApi`'s `UseSpec` (topics, request/response
payloads, broadcast events, and validation rules). It is the Benzene equivalent of `UseSwaggerUI`,
but topic-centric rather than path-centric.

This package renders the spec; it does **not** generate it. Generation lives in
`Benzene.Schema.OpenApi` (the `spec` topic / `GET /spec?type=benzene`).

## Key types
- `SpecUiPage` — transport-agnostic accessor for the viewer HTML.
  - `GetHtml()` — the page as-is (falls back to an embedded sample spec, or a `?url=` query param).
  - `GetHtml(string specUrl)` — injects a `data-spec-url` onto the document root so the page fetches
    and renders that spec on load.
- `SpecUiMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext` — transport-
  agnostic HTTP middleware. On a GET/HEAD to its path it writes the page as `text/html` and
  short-circuits; otherwise it calls `next`.
- `SpecUiExtensions.UseSpecUi<TContext>(this IMiddlewarePipelineBuilder<TContext>, path = "/spec-ui", specUrl = "/spec?type=benzene")`
  — registers the middleware on any Benzene HTTP pipeline.

## Why it isn't ASP.NET-specific
Most Benzene services run serverless (Lambda / Azure Functions), so serving must not bind to
ASP.NET. `SpecUiMiddleware` emits the response by driving `IBenzeneResponseAdapter<TContext>`
directly — `SetStatusCode("200")` → `SetContentType("text/html")` → `SetBody(html)` →
`FinalizeAsync(context)` — and deliberately does **not** route through the message-result path
(`IMessageHandlerResultSetter.SetResultAsync`), because that path's body handler forces
`application/json` on ASP.NET. This is the same short-circuit shape as `CorsMiddleware<TContext>`
and works identically on API Gateway, Azure Functions, ASP.NET Core, and self-host.

## The viewer (`spec-ui.html`)
- A single self-contained HTML file (inline CSS + vanilla JS, no external requests) embedded as a
  resource (`LogicalName` `Benzene.Spec.Ui.spec-ui.html`).
- Resolves `$ref`s into `components.schemas`; renders each topic as an expandable "operation" with
  its request/response payload tables, required-field emphasis, and validation constraint chips
  (`format`, `enum`, `minLength`/`maxLength`, `minimum`/`maximum`, `pattern`, `nullable`).
- Renders the per-topic/per-event `example` payload the `benzene` spec carries (generated
  server-side by `Benzene.Schema.OpenApi.Examples.ExamplePayloadBuilder` during spec build),
  pretty-printed with a copy button (`navigator.clipboard` with an `execCommand` fallback).
- Loads a spec from, in precedence order: `?url=` query param → `data-spec-url` on the document root
  → embedded sample. Theme-aware (light/dark), with a search filter and a "Load spec" dialog.

## When to use this package
- To give a running Benzene service a browsable spec page, alongside its `spec` endpoint.
- `UseSpecUi` is the turnkey path on any HTTP transport (Lambda, Functions, ASP.NET, self-host).
  Any transport can also serve `SpecUiPage.GetHtml(...)` directly.

## Dependencies
- `Benzene.Http` (project reference) — for the transport-agnostic HTTP abstractions
  (`IHttpContext`, `IHttpRequestAdapter`, `IBenzeneResponseAdapter`, the middleware pipeline). No
  ASP.NET / web-framework dependency. `SpecUiPage` alone has no Benzene dependencies at all.

## Conventions
- Point the UI at the `benzene` spec type (`/spec?type=benzene`) — it is designed around the
  topic/payload/validation shape of that format, not `openapi`/`asyncapi`.

## Tests
- `test/Benzene.Core.Test/SpecUi/SpecUiPageTest.cs` — `GetHtml()`/`GetHtml(specUrl)`: embedded
  resource loads, `data-spec-url` injection and HTML-encoding, null/whitespace fallback.
- `test/Benzene.Core.Test/SpecUi/SpecUiMiddlewareTest.cs` — GET/HEAD-to-matching-path
  short-circuits (writes the page via `IBenzeneResponseAdapter`, never calls `next`); any other
  method or path falls through to `next`; path normalization (case, leading/trailing slash).
  Uses a trivial `FakeHttpContext : IHttpContext` (the interface is a pure marker) with Moq'd
  `IHttpRequestAdapter`/`IBenzeneResponseAdapter` — no real transport needed, unlike
  `Benzene.SelfHost.Http`'s tests.
- Keep the viewer dependency-free and self-contained (no CDN/webfont/script references) so it works
  offline and behind strict CSPs, and can be embedded as a single resource.
