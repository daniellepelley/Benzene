# Spec

The spec topic allows a Hex service to serve up schemas such as OpenApi, AsyncApi and a custom format used to generate code.

This functionality can be added to a Benzene message pipeline using the UseSpec middleware extensions. The topic for this should be set to “spec”.


```csharp
  app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
      .UseSpec("spec")
```

## Making a Spec Request


```json
{
  "topic": "spec",
  "message" : "{\"type\":\"asyncapi\",\"format\":\"json\"}"
}
```
 
| Field | Options |
| ----- | ------- |
| Type  | “asyncapi”, ”openapi”, ”benzene” |
| Format | “json”, ”yaml” |

## Example payloads

The `benzene` format includes a generated `example` on every request topic and broadcast event —
a ready-made payload derived from the request/message schema. Examples are deterministic (the
same schema always produces the same example) and respect the validation metadata the spec
carries: schema `example`/`default`/`enum` values win when present, string `format`s
(`uuid`, `date-time`, `date`, `email`, `uri`) produce format-shaped values, strings are sized
within `minLength`/`maxLength`, and numbers are clamped into `minimum`/`maximum`.

```json
{
  "topic": "tenant:create",
  "request": { "$ref": "#/components/schemas/CreateTenantMessage" },
  "response": { "$ref": "#/components/schemas/TenantDto" },
  "example": { "name": "value", "crn": "value" }
}
```

Consumers — the [Spec UI](spec-ui.md), code generators, or anyone with `curl` — can use the
example as-is to send a test message to the service. Generation lives in
`Benzene.Schema.OpenApi.Examples.ExamplePayloadBuilder`.

## Message endpoint advertisement

When the service exposes a BenzeneMessage-over-HTTP endpoint (`UseBenzeneMessage` — see
[Payload Testing](payload-testing.md)), the `benzene` format advertises it as an optional
top-level `messageEndpoint` field:

```json
{ "openapi": "3.0.1", "info": { }, "messageEndpoint": "/benzene-message", "requests": [ ] }
```

Consumers feature-detect send capability on this field — no field means the service accepts
messages only through its normal transports.

