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

## Serving it

`UseSpecUi` is a transport-agnostic Benzene HTTP middleware — it works on **any** HTTP pipeline
(AWS Lambda API Gateway, Azure Functions, ASP.NET Core, or the self-host server), which matters
because most Benzene services run serverless. It has no ASP.NET dependency; it writes the page
straight through Benzene's response adapter with `text/html`.

Add it to your HTTP pipeline, before the message handlers, alongside a `spec` endpoint:

```csharp
using Benzene.Schema.OpenApi;   // UseSpec
using Benzene.Spec.Ui;          // UseSpecUi

// AWS Lambda API Gateway (the same call works on Azure Functions, ASP.NET, self-host).
app.UseApiGateway(http => http
    .UseSpecUi()                                   // serves GET /spec-ui
    .UseSpec()                                     // serves the spec on GET /spec (see docs/spec.md)
    .UseMessageHandlers(router => router.UseFluentValidation())
);

// Customise the path and/or the spec URL it fetches:
app.UseApiGateway(http => http
    .UseSpecUi("/docs", "/spec?type=benzene")
    .UseSpec()
    .UseMessageHandlers(router => router)
);
```

Browse to `/spec-ui`. The page fetches `/spec?type=benzene` and renders it.

> Use the **`benzene`** spec type — the UI is designed around its topic/payload/validation shape,
> not `openapi`/`asyncapi`.

## Serving it yourself

The viewer is a single self-contained HTML file with no external requests. If you aren't using the
middleware, get the markup from `SpecUiPage` and write it to any HTTP response:

```csharp
var html = SpecUiPage.GetHtml("/spec?type=benzene"); // inject the spec URL onto the page
// write `html` with content-type "text/html"
```

## Loading a spec without wiring

Open the page standalone and either append `?url=<spec-json-url>`, or use the **Load spec** button
to paste spec JSON directly. With no spec supplied it renders a built-in sample so you can see the
layout.
