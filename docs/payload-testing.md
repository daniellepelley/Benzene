# Payload Testing

AWS ships a Lambda Test Tool for firing test payloads at a locally running Lambda; Swagger UI lets
you construct and send test requests at an HTTP API. Benzene's equivalent is **topic-centric**:
construct a payload for a *topic*, and send it into the running service through the same pipeline
every transport uses — routing, validation, middleware, handler — regardless of whether that topic
also has an HTTP mapping or is normally fed by SQS, SNS, EventBridge, Kafka, or Event Hubs.

Three pieces work together:

1. **Example payloads in the spec** — the `benzene` spec carries a generated, validation-aware
   `example` for every topic and event (see [Spec](spec.md)).
2. **The message endpoint** (`UseBenzeneMessage` on any HTTP pipeline, this page) — accepts a
   POSTed BenzeneMessage envelope and dispatches it into the message pipeline.
3. **The [Spec UI](spec-ui.md)** — renders the examples and, when the service exposes a message
   endpoint, offers a *Try it* panel to edit and send payloads from the browser.

## The message endpoint

`UseBenzeneMessage` (in `Benzene.Http`, namespace `Benzene.Http.BenzeneMessage`) is the HTTP
equivalent of the direct AWS Lambda invoke path — same name, same overload shapes. It works on
**any** Benzene HTTP transport (AWS Lambda API Gateway, Azure Functions, ASP.NET Core, self-host)
because it drives the transport-neutral request/response adapters directly.

```csharp
using Benzene.Http.BenzeneMessage;

// Inline pipeline — serves POST /benzene-message
app.UseApiGateway(apiGateway => apiGateway
    .UseBenzeneMessage(messageApp => messageApp
        .UseMessageHandlers(router => router.UseFluentValidation()))
    .UseMessageHandlers(router => router.UseFluentValidation())
);

// Or share one message pipeline across adapters (direct Lambda invoke + HTTP):
var benzeneMessagePipeline = aws.Create<BenzeneMessageContext>()
    .UseMessageHandlers(router => router.UseFluentValidation());

aws.UseBenzeneMessage(benzeneMessagePipeline);                       // direct Lambda invoke
aws.UseApiGateway(apiGateway => apiGateway
    .UseBenzeneMessage(benzeneMessagePipeline)                       // HTTP endpoint
    .UseMessageHandlers());
```

### The wire contract

POST a BenzeneMessage envelope; the `body` is the payload **as a JSON string**:

```bash
curl -X POST http://localhost:8080/benzene-message \
  -H "content-type: application/json" \
  -d '{ "topic": "order:create", "headers": {}, "body": "{\"customerId\":\"11111111-1111-1111-1111-111111111111\"}" }'
```

The response is the response envelope, with the HTTP status mapped from the envelope's status
(`Ok` → 200, `ValidationError` → 422, `NotFound` → 404, …):

```json
{ "statusCode": "Ok", "headers": { }, "body": "{\"orderId\":\"...\"}" }
```

The envelope itself is always plain JSON (camelCase), independent of whatever payload formats the
app's own serializer negotiates. A malformed or topic-less envelope gets a `BadRequest` envelope
back with a 400.

### Options

```csharp
apiGateway.UseBenzeneMessage(new BenzeneMessageHttpOptions
{
    Path = "/admin/benzene-message",              // default: /benzene-message
    TopicFilter = topic => topic.StartsWith("order:")  // reject → NotFound envelope, 404
}, messageApp => messageApp.UseMessageHandlers());
```

### Spec advertisement

Registering the endpoint also registers an `IBenzeneMessageHttpEndpointInfo`, and the `benzene`
spec advertises it as a top-level `messageEndpoint` field. Consumers — the Spec UI's *Try it*
panel in particular — feature-detect send capability on that field: no field, no send button.

## ⚠ Security

The endpoint dispatches **every topic the pipeline routes**, including topics that have no HTTP
mapping and would otherwise only be reachable from a queue or an event bus. Treat it accordingly:

- It is **opt-in only** — nothing registers it implicitly.
- Intended for **local development** and protected/admin environments.
- Restrict what it can reach with `TopicFilter`, and compose your authentication middleware
  **before** it in the pipeline.
- Do **not** expose it unauthenticated in production.
