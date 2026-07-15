# Benzene.Avro

## What this package does
Apache Avro serialization integration for Benzene. Avro is a compact binary format popular in
finance/data streaming (Kafka) for its size and schema evolution. This package is the binary
counterpart to `Benzene.Xml`: it plugs an `IMediaFormat<TContext>` (`application/avro`) into the
request/response content-negotiation pipeline and, because Avro is binary, is the natural exercise of
the byte-oriented `IPayloadSerializer` path (Phase 4).

## Key types
- `AvroSerializer : ISerializer, IPayloadSerializer` — real Avro binary on the byte-oriented members
  (`Serialize(Type, object, IBufferWriter<byte>)` / `Deserialize(Type, ReadOnlySpan<byte>)`), which is
  the path `RequestMapper` takes when a transport registers `IMessageBodyBytesGetter`. The string
  `ISerializer` members Base64-encode the same Avro bytes (rather than throwing, the contract
  `IPayloadSerializer` permits for binary formats), so Avro also works over string-only bodies and the
  current string-based response path.
- `AvroMediaFormat<TContext> : AcceptHeaderMediaFormatBase<TContext>` — `application/avro`, negotiated
  by `content-type` (read) / `accept` (write) like every other format.
- `AvroOptions` — schema configuration (see below).
- `IAvroSchemaResolver` / `AvroSchemaResolver` — resolves and caches the Avro schema per CLR type.
- `AvroSchemaGenerator` (internal) — reflection-based CLR-type → `.avsc` generator.
- `AvroDatumConverter` (internal) — maps POCOs ↔ Avro `GenericRecord`/array/primitive datums.

## Schemas — with or without (configurable)
Avro is schema-based, unlike JSON/XML. `AvroOptions` supports both models, mixable per type:
- **Reflection (default, schemaless to the caller):** the schema is inferred from a type's public
  read/write properties. On by default.
- **Explicit schema:** register an `.avsc` per type — `AddAvro(o => o.RegisterSchema<OrderDto>("{...}"))`
  — matching the schema-registry model common in finance/Kafka. An explicit registration wins over
  reflection for that type. Set `o.UseReflectionSchemas = false` to require an explicit schema for
  every type (unregistered types then throw).

## Registration
```csharp
// reflection schemas (default)
pipeline.UseAvro<MyContext>();

// or with explicit schemas / options
pipeline.UseAvro<MyContext>(o => o
    .RegisterSchema<OrderDto>(orderAvsc));
```
`AddAvro(...)` registers the shared `AvroSerializer` and `AvroMediaFormat<>` as an
`IMediaFormat<TContext>`; content negotiation then selects it whenever `application/avro` is requested.

## Reflection type mapping (v1)
`bool→boolean`, integral(≤32-bit)`→int`, `long/ulong→long`, `float→float`, `double→double`,
`byte[]→bytes`, and `string/Guid/DateTime/DateTimeOffset/decimal/enum→string` (stringified to preserve
precision/round-tripping for money and timestamps). Nested classes → Avro records; `IEnumerable<T>` /
arrays → Avro arrays. Reference-typed and `Nullable<T>` members become a `["null", X]` union so nulls
round-trip. For full Avro logical types (native `decimal`, `timestamp-millis`, `uuid`) register an
explicit schema.

## Dependencies
- **Apache.Avro** — the official Apache Avro .NET library (binary encode/decode + schema model).
- **Benzene.Abstractions.MessageHandlers** / **Benzene.Core.MessageHandlers** — `IMediaFormat`,
  `AcceptHeaderMediaFormatBase`, `ISerializer`/`IPayloadSerializer`.

## Conventions
- Registered as an `IMediaFormat<TContext>` (not by replacing the default `ISerializer`), so Avro is
  negotiated alongside JSON/XML rather than replacing the process default.
- The serializer is a stateless singleton; schema parsing/generation is cached per type.
