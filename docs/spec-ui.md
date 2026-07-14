# Spec UI

Benzene can serve its own message spec as JSON (see [Spec](spec.md)) — topics, request/response
payloads, broadcast events, and validation rules. **`Benzene.Spec.Ui`** renders that spec in a
browsable, Swagger-UI-style page. It's the Benzene equivalent of `UseSwaggerUI`, but organised
around message **topics** rather than URL paths.

## What it shows

- **Message topics** — each request/response topic as an expandable card, with any HTTP mappings,
  and the request and response payloads rendered as property tables.
- **Broadcast events** — fire-and-forget topics and their message payloads.
- **Schemas** — the full `components.schemas` catalogue.
- **Validation rules** — required fields, `format` (e.g. `uuid`, `email`), `enum` values, string
  length and numeric range, `pattern`, and `nullable` — shown as inline constraint chips, sourced
  from the same validation metadata the `benzene` spec projects onto each schema.

## Serving it (ASP.NET Core)

Expose a spec endpoint with `UseSpec` and serve the UI alongside it:

```csharp
using Benzene.Spec.Ui;

// The Benzene message pipeline exposes the spec on the "spec" topic,
// mapped to GET /spec (see docs/spec.md).
app.UseBenzene(benzene => benzene
    .UseHttp(http => http
        .UseSpec()
        .UseMessageHandlers(x => x.UseFluentValidation())
    )
);

// Serve the viewer at /spec-ui, pointed at the benzene-format spec.
app.UseBenzeneSpecUi();                       // defaults: "/spec-ui", "/spec?type=benzene"
// or customise:
app.UseBenzeneSpecUi("/docs", "/spec?type=benzene");
```

Browse to `/spec-ui`. The page fetches `/spec?type=benzene` and renders it.

> Use the **`benzene`** spec type — the UI is designed around its topic/payload/validation shape,
> not `openapi`/`asyncapi`.

## Other transports

The viewer is a single self-contained HTML file with no external requests, so any transport can
serve it. Get the markup from `SpecUiPage`:

```csharp
var html = SpecUiPage.GetHtml("/spec?type=benzene"); // inject the spec URL
// write `html` to your HTTP response with content-type "text/html"
```

## Loading a spec without wiring

Open the page standalone and either append `?url=<spec-json-url>`, or use the **Load spec** button
to paste spec JSON directly. With no spec supplied it renders a built-in sample so you can see the
layout.
