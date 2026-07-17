# Benzene.Core.Versioning

## What this package does
Payload multi-versioning support: casts a payload from the schema version it was published with
to the schema version the consuming message handlers understand - upcasting older payloads
forward (V1 -> V2) or downcasting newer payloads back (V2 -> V1), composing multi-step chains
(V1 -> V2 -> V3) automatically when no direct caster is registered.

Roadmap context: `version` is planned as a header on the CloudEvents-style payload standard.
When a service runs in multi-versioned mode, the version is read off the incoming payload's
header, the correct casting chain is assembled, and the payload is cast up (or down) to whatever
version the registered message handlers support. This package is the casting engine for that;
the transport/header wiring lands separately.

## Key types/interfaces

### Casters (`Casters/`)
- `ICaster<TFrom, TTo>` - casts one payload type to another
- `FuncCaster<TFrom, TTo>` - wraps a `Func<TFrom, TTo>`
- `CompositeCaster<TFrom, TIntermediate, TTo>` - chains two casters

### Caster building (`CasterBuilder/`)
- `CasterFactory<TFrom, TTo>` - fluent entry point; builds an `ICaster` via compiled expression
  trees that map properties by name. Supports `RegisterInitValue` (seed a property added in the
  target schema - also overrides mapped properties), `RegisterSubTypeInitValue`, and
  `RegisterTypeMapping` (map renamed types explicitly)
- `CasterFuncBuilder` - compiles the mapping `Func`: same-name property copy, nested classes,
  `List<T>`/generic `IEnumerable<T>` properties, enums by value, nullables, and polymorphic
  base-type properties (per-derived-type dispatch)
- `SchemaTypeMatcher` - resolves the target type for a source type: explicit registrations first,
  then same-simple-name lookup in the target type's namespace/assembly. **Convention: schema
  versions keep identical type names in per-version namespaces** (e.g. `Contracts.V1.Order` /
  `Contracts.V2.Order`)

### Schema registry (`Schemas/`)
- `ISchemaCaster` / `SchemaCaster<TFrom, TTo>` - a caster tagged with topic + from/to schema names
- `SchemaCastDefinition` - topic + FromSchema + ToSchema identity
- `ISchemaCasters` / `SchemaCasters` - lookup of casters by (from, to, topic)
- `SchemaCastersBuilder` / `SchemaCasterBuilder<TFrom, TTo>` - fluent registration
- `SchemaCastDefinitionsExpander` - given the required from/to version pairs
  (`PayloadSchemaVersions`), reuses direct casters and BFS-composes multi-step chains; throws at
  expansion time when no path exists (fail fast at startup)
- `SchemaCasterExtensions` - DI registration: `RegisterSchemaCastDefinitions` (register individual
  casters) + `RegisterPayloadSchemaVersions` (register the expanded `ISchemaCasters` singleton).
  Collects registered casters via `IServiceResolver.GetServices<ISchemaCaster>()`

### Version-aware deserialization (`Deserializer/`)
- `IPayloadDeserializer` / `PayloadDeserializer` - reads schema version + topic off a
  `JsonElement` (via `IPayloadFields`), compares with the version the service wants
  (`IPayloadSchemaVersionLookUp`), deserializes directly on match or runs the casting chain
- `IPayloadFields` - transport-specific extraction of version/topic from the raw payload
- `PayloadSchemaVersions` - declares, per topic, which versions may arrive and which the
  handlers support

## When to use this package
- When consumers and producers of a topic evolve payload schemas at different speeds
- When message handlers should only ever see one schema version while older/newer payloads
  keep flowing

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - DI abstractions (`IBenzeneServiceContainer`, `IServiceResolver`)

## Important conventions
- Schema versions are plain strings ("V1", "V2"...) scoped per topic
- Casting is configured at startup and compiled once; missing paths fail at expansion, not
  per message
- Tests live in `test/Benzene.Core.Test/Core/Versioning/`
