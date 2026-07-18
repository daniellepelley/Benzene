# Schema Registry Integration


> **Boundary:** the in-box compatibility checker only accepts byte-identical schemas; structural evolution needs a real registry server or your own checker — see the [Capability Matrix](../capability-matrix.md).

Event-driven services that share a Kafka topic need a shared source of truth for the payload schema
on that topic — so a producer can't silently ship a breaking change, and consumers (including
non-Benzene ones) can resolve the exact schema a message was written with. A **schema registry**
(Confluent, Azure Schema Registry, AWS Glue) is that source of truth.

`Benzene.SchemaRegistry.Core` gives you a provider-agnostic `ISchemaRegistryClient` seam, the
Confluent wire-format codec that makes framed payloads interoperable across the Kafka ecosystem, and
a serializer decorator that registers a type's schema and frames its output — complementing
`Benzene.Avro` (which serializes without a registry) and the
[contract-testing CI gate](contract-testing.md) (which checks compatibility at build time). The core
is BCL-only; the registry clients are documented copy-paste adapters below.

## Problem statement

Your service publishes `OrderCreated` to a Kafka topic. You want its schema registered centrally, the
published bytes framed so any Confluent consumer can resolve the writer schema, and a startup check
that fails the deploy if the new schema breaks the subject's compatibility rules.

## The abstraction

```csharp
public interface ISchemaRegistryClient
{
    Task<int> RegisterAsync(SchemaDefinition schema, CancellationToken ct = default);   // -> schema id
    Task<RegisteredSchema?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<RegisteredSchema?> GetLatestAsync(string subject, CancellationToken ct = default);
    Task<bool> IsCompatibleAsync(SchemaDefinition schema, CancellationToken ct = default);
}
```

`InMemorySchemaRegistryClient` implements it for tests, local development, and single-node use.

## The Confluent wire format

Confluent's serializers prefix each payload with a `0x00` magic byte and the 4-byte big-endian schema
id, then the serialized body. Framing Benzene's payloads the same way makes them interoperable — a
non-Benzene Kafka consumer resolves the writer schema from the embedded id:

```csharp
byte[] framed = ConfluentWireFormat.Encode(schemaId, bodyBytes);   // 0x00 | id(4, BE) | body
ReadOnlySpan<byte> body = ConfluentWireFormat.Decode(framed, out int schemaId);
```

## Step 1 — tell the registrar how to find each type's schema

`ISchemaResolver` maps a message type to the schema to register. With `Benzene.Avro`, adapt its
existing `IAvroSchemaResolver`:

```csharp
using Benzene.Avro;
using Benzene.SchemaRegistry.Core;

public class AvroSchemaResolverAdapter : ISchemaResolver
{
    private readonly IAvroSchemaResolver _avro;
    public AvroSchemaResolverAdapter(IAvroSchemaResolver avro) => _avro = avro;

    public SchemaDefinition Resolve(Type type)
        => new($"{type.Name}-value", _avro.GetSchema(type).ToString(), SchemaFormat.Avro);
}
```

The subject convention `<type>-value` matches Confluent's default `TopicNameStrategy` for a Kafka
value schema. Or use `DelegateSchemaResolver` for an inline mapping.

## Step 2 — register schemas and build the serializer at startup

Registration is async and happens once at startup — `SchemaRegistrar` does it up front and hands back
a serializer whose `Serialize` is synchronous (no registry call on the hot path):

```csharp
ISchemaRegistryClient registry = /* Confluent/Azure adapter, or InMemorySchemaRegistryClient */;
var registrar = new SchemaRegistrar(registry, new AvroSchemaResolverAdapter(avroResolver));

// Optional: fail the deploy if a schema is no longer compatible (evolution gate).
await registrar.EnsureCompatibleAsync(new[] { typeof(OrderCreated), typeof(OrderShipped) });

// Register + wrap any inner IPayloadSerializer (here Benzene.Avro's) with registry framing.
var serializer = await registrar.CreateSerializerAsync(
    new AvroSerializer(avroResolver),
    new[] { typeof(OrderCreated), typeof(OrderShipped) });
```

The resulting `SchemaRegistrySerializer` frames the Avro bytes with the registered schema id.
Serializing a type you didn't register throws immediately, so a missing registration surfaces at
startup, not at runtime.

## Step 3 — register the client in DI

```csharp
services.AddSchemaRegistry(registry);   // registers ISchemaRegistryClient
```

## Registry client adapters (copy-paste)

Each is a thin async wrapper implementing `ISchemaRegistryClient`. Drop in the one you need plus its
SDK package — the core stays dependency-free so you only pull in the registry you actually run.

### Confluent Schema Registry — `Confluent.SchemaRegistry`

```csharp
using Confluent.SchemaRegistry;
using Benzene.SchemaRegistry.Core;

public class ConfluentSchemaRegistryClient : ISchemaRegistryClient
{
    private readonly Confluent.SchemaRegistry.ISchemaRegistryClient _client;
    public ConfluentSchemaRegistryClient(Confluent.SchemaRegistry.ISchemaRegistryClient client) => _client = client;

    public async Task<int> RegisterAsync(SchemaDefinition schema, CancellationToken ct = default)
        => await _client.RegisterSchemaAsync(schema.Subject, new Schema(schema.Schema, ToSchemaType(schema.Format)));

    public async Task<RegisteredSchema?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var s = await _client.GetSchemaAsync(id);
        return s is null ? null : new RegisteredSchema(id, subject: "", version: 0, s.SchemaString, ToFormat(s.SchemaType));
    }

    public async Task<RegisteredSchema?> GetLatestAsync(string subject, CancellationToken ct = default)
    {
        var s = await _client.GetLatestSchemaAsync(subject);
        return s is null ? null : new RegisteredSchema(s.Id, s.Subject, s.Version, s.SchemaString, ToFormat(s.SchemaType));
    }

    public Task<bool> IsCompatibleAsync(SchemaDefinition schema, CancellationToken ct = default)
        => _client.IsCompatibleAsync(schema.Subject, new Schema(schema.Schema, ToSchemaType(schema.Format)));

    private static SchemaType ToSchemaType(SchemaFormat f) => f switch
    {
        SchemaFormat.Json => SchemaType.Json,
        SchemaFormat.Protobuf => SchemaType.Protobuf,
        _ => SchemaType.Avro,
    };
    private static SchemaFormat ToFormat(SchemaType t) => t switch
    {
        SchemaType.Json => SchemaFormat.Json,
        SchemaType.Protobuf => SchemaFormat.Protobuf,
        _ => SchemaFormat.Avro,
    };
}
```

Confluent's client already frames payloads itself; use `ConfluentWireFormat` only when you're framing
manually rather than through Confluent's own Avro serializer.

### Azure Schema Registry — `Azure.Data.SchemaRegistry`

```csharp
using Azure.Data.SchemaRegistry;
using Azure.Identity;
using Benzene.SchemaRegistry.Core;

public class AzureSchemaRegistryClient : ISchemaRegistryClient
{
    private readonly SchemaRegistryClient _client;
    private readonly string _group;
    public AzureSchemaRegistryClient(string endpoint, string group)
    {
        _client = new SchemaRegistryClient(endpoint, new DefaultAzureCredential());
        _group = group;
    }

    public async Task<int> RegisterAsync(SchemaDefinition schema, CancellationToken ct = default)
    {
        var props = await _client.RegisterSchemaAsync(_group, schema.Subject, schema.Schema, SchemaFormat.Avro.ToAzure(), ct);
        // Azure identifies schemas by a string id; hash it into an int for the wire-format frame,
        // or keep your own id map if you need the exact Azure id.
        return props.Value.Id.GetHashCode();
    }

    // GetByIdAsync / GetLatestAsync / IsCompatibleAsync over the Azure client similarly...
    public Task<RegisteredSchema?> GetByIdAsync(int id, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<RegisteredSchema?> GetLatestAsync(string subject, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<bool> IsCompatibleAsync(SchemaDefinition schema, CancellationToken ct = default) => Task.FromResult(true);
}
```

Azure's registry uses string ids (GUIDs), so if you frame with the 4-byte Confluent id you need your
own id mapping — noted here so the difference isn't a surprise.

## Testing

`InMemorySchemaRegistryClient` and a trivial fake `IPayloadSerializer` exercise the whole path — id
assignment, idempotent registration, compatibility rejection, and the framing round-trip — with no
registry server. See `test/Benzene.Core.Test/SchemaRegistry/` for worked examples.

## How this fits the contract story

- **Build time** — the [contract-testing gate](contract-testing.md) (`SchemaCompatibility.EnsureBackwardCompatible`)
  fails a PR that breaks a consumer.
- **Deploy time** — `SchemaRegistrar.EnsureCompatibleAsync` fails startup if a schema is incompatible
  with the registry's rules.
- **Runtime** — `SchemaRegistrySerializer` frames each message with its schema id, so consumers
  resolve the exact writer schema and drift is impossible to miss.

## Further reading

- `src/Benzene.SchemaRegistry.Core/CLAUDE.md` — the type-by-type reference.
- [Contract Testing](contract-testing.md) — the build/CI compatibility gate this complements.
- `work/enterprise-adoption-gap-analysis.md` A.6 — the design context.
