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
- `SpecUiExtensions.UseBenzeneSpecUi(this IApplicationBuilder, path = "/spec-ui", specUrl = "/spec?type=benzene")`
  — ASP.NET Core wiring that serves the page with `text/html` at `path`.

## The viewer (`spec-ui.html`)
- A single self-contained HTML file (inline CSS + vanilla JS, no external requests) embedded as a
  resource (`LogicalName` `Benzene.Spec.Ui.spec-ui.html`).
- Resolves `$ref`s into `components.schemas`; renders each topic as an expandable "operation" with
  its request/response payload tables, required-field emphasis, and validation constraint chips
  (`format`, `enum`, `minLength`/`maxLength`, `minimum`/`maximum`, `pattern`, `nullable`).
- Loads a spec from, in precedence order: `?url=` query param → `data-spec-url` on the document root
  → embedded sample. Theme-aware (light/dark), with a search filter and a "Load spec" dialog.

## When to use this package
- To give a running Benzene service a browsable spec page, alongside its `spec` endpoint.
- Any HTTP transport can serve `SpecUiPage.GetHtml(...)` directly; `UseBenzeneSpecUi` is the
  turnkey path for ASP.NET Core.

## Dependencies
- `Microsoft.AspNetCore.App` (framework reference) — for the `UseBenzeneSpecUi` `IApplicationBuilder`
  extension. `SpecUiPage` itself has no Benzene or ASP.NET dependencies.

## Conventions
- Point the UI at the `benzene` spec type (`/spec?type=benzene`) — it is designed around the
  topic/payload/validation shape of that format, not `openapi`/`asyncapi`.
- Keep the viewer dependency-free and self-contained (no CDN/webfont/script references) so it works
  offline and behind strict CSPs, and can be embedded as a single resource.
