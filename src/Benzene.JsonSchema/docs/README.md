# JSON Schema Validation
JSON Schema is the standard, language-neutral way to describe and validate the shape of JSON documents. Validating with JSON Schema means the contract *is* the validator: the same schema document that describes a payload in your spec can reject non-conforming payloads at the door — with no C# validator classes to write, and no way for the two to drift apart.

### Integration with Benzene
JSON Schema validation runs as pipeline middleware over the **raw request body**, before deserialization — the natural place for a document validator (it checks the wire JSON itself, so it also catches malformed or missing bodies).

For each message it obtains a schema for the current topic and evaluates the body against it. A failing body short-circuits with a **ValidationError** result whose payload is an array of property-scoped messages — the same failure contract as `Benzene.FluentValidation` and `Benzene.DataAnnotations`:

```json
["/name: Value is longer than 5 characters", "/lines/0/sku: Required properties [\"sku\"] are not present"]
```

A `null` schema for a topic means "no validation" and the message passes through.

```csharp
.UseJsonSchema()
.UseMessageHandlers()
```

### Where schemas come from
- **Generated (default):** `DefaultJsonSchemaProvider` derives a schema from the registered handler's request type (JsonSchema.Net.Generation, draft 2020-12, camelCase).
- **Bring your own:** register hand-authored schema documents per request type — the same documents you can serve from the spec via `Benzene.Schema.OpenApi`'s `SuppliedSchemaCatalog`, so published contract and runtime validation stay aligned:

```csharp
var schemas = new SuppliedJsonSchemaCatalog()
    .AddJson(typeof(CreateOrderMessage), File.ReadAllText("schemas/create-order.json"));

services.UsingBenzene(x => x.AddSuppliedJsonSchemas(schemas));
```

- **Fully custom:** implement `IJsonSchemaProvider<TContext>` to source schemas from anywhere (a registry service, embedded resources, per-tenant stores).
