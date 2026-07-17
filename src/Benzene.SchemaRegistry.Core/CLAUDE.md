# Benzene.SchemaRegistry.Core

## What this package does
The neutral **schema-registry** integration (gap-analysis A.6). Lets a service register its event
payload schemas centrally and frame published messages so the wider Kafka ecosystem can resolve the
writer schema — complementing `Benzene.Avro` (which serializes registry-less) and the A.2 contract
gate (build-time compatibility) with a runtime registry story. Ships the mechanism BCL-only — no
Confluent/Azure SDK dependency; those registry clients implement the same one interface.

## Key types
- `ISchemaRegistryClient` — the neutral seam: `RegisterAsync(SchemaDefinition)→int id` (idempotent
  for an identical schema), `GetByIdAsync`, `GetLatestAsync(subject)`, `IsCompatibleAsync` (evolution
  check). A provider adapter (Confluent, Azure) implements it; the cookbook shows copy-paste versions.
- `SchemaDefinition` (subject + schema text + `SchemaFormat`), `RegisteredSchema` (adds id + version),
  `SchemaFormat` (Avro/Json/Protobuf), `SchemaCompatibilityMode` (None/Backward/Forward/Full).
- `InMemorySchemaRegistryClient` — reference impl + test double: monotonic ids, per-subject versions,
  idempotent re-registration, compatibility via a pluggable `ISchemaCompatibilityChecker`. Single
  process only (doesn't coordinate ids across instances — use a shared registry there).
- `ISchemaCompatibilityChecker` / `TextualSchemaCompatibilityChecker` — the default is deliberately
  conservative (first schema always OK; `None` accepts anything; otherwise must be textually
  identical), so it never *falsely* approves a structural change. Supply a format-aware checker, or
  rely on the registry server's own check, for real Avro/JSON evolution rules.
- `ConfluentWireFormat` — the interop-critical byte codec: `Encode(schemaId, payload)` prepends the
  `0x00` magic byte + 4-byte **big-endian** schema id; `Decode(framed, out id)` validates and strips
  it. This is exactly the framing Confluent producers/consumers expect, so Benzene payloads
  interoperate with non-Benzene Kafka consumers. `HeaderLength` = 5. BCL-only, fully tested.
- `SchemaRegistrySerializer` — an `IPayloadSerializer` decorator that frames an inner serializer's
  output with `ConfluentWireFormat` using a per-type schema-id map. Works over **any** inner payload
  serializer (Avro/JSON/MessagePack) — it adds the registry framing, not a format. Schema ids are
  resolved once at startup into the map (so serialize stays synchronous, no registry call on the hot
  path); an unregistered type throws. Base64-armors the framed bytes on the string path, like
  `Benzene.Avro`, so it flows through string-body pipelines too.
- `ISchemaResolver` / `DelegateSchemaResolver` — maps a CLR type to its `SchemaDefinition`. Pluggable
  and format-specific (an adapter over `Benzene.Avro`'s `IAvroSchemaResolver` supplies the Avro
  schema) — keeps this package Avro-free.
- `SchemaRegistrar` — startup helper: `RegisterAsync(types)→id map`, `EnsureCompatibleAsync(types)`
  (fail-fast evolution gate, lists all incompatible subjects), and `CreateSerializerAsync(inner,
  types)→SchemaRegistrySerializer`.
- `SchemaIncompatibleException`, `Extensions.AddSchemaRegistry(client)`.

## Conventions / design boundary
- **BCL-only.** The registry *clients* are the vendor-coupled, un-CI-testable (needs a live registry)
  part — kept out. Same "core owns the mechanism/wire format, adapters at the edge" split as
  `Benzene.Configuration.Core`/`Benzene.Auth.Core`; adapters are documented copy-paste.
- **Register + resolve ids at startup, async; serialize sync.** No sync-over-async, no per-message
  registry call — `SchemaRegistrar` does the async work up front and hands the serializer a fixed map.
- The framing (`ConfluentWireFormat`) is the genuinely reusable, interop-critical piece and is fully
  unit-tested; a registry adapter is a thin async wrapper the docs show.

## Docs
- Cookbook `docs/cookbooks/schema-registry.md` — wiring, the Confluent framing, the Avro `ISchemaResolver`
  adapter, and copy-paste Confluent/Azure `ISchemaRegistryClient` adapters.

## Tests
- `test/Benzene.Core.Test/SchemaRegistry/ConfluentWireFormatTest.cs` — magic byte + big-endian id +
  body, round-trip, buffer-writer parity, too-short/wrong-magic throw.
- `test/Benzene.Core.Test/SchemaRegistry/InMemorySchemaRegistryClientTest.cs` — id assignment,
  idempotent re-register, versions, get-by-id/latest, Backward-rejects vs. None-allows, first-schema
  always compatible.
- `test/Benzene.Core.Test/SchemaRegistry/SchemaRegistrySerializerTest.cs` — registrar builds the map,
  serializer frames with the registered id, string-path Base64 round-trip, unregistered-type throws,
  `EnsureCompatibleAsync` throws on a changed schema under Backward.
