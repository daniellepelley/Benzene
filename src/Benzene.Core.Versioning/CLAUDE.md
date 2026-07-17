# Benzene.Core.Versioning

## What this package does
Payload multi-versioning support: casts a payload from the schema version it was published with
to the schema version the consuming message handlers understand - upcasting older payloads
forward (V1 -> V2) or downcasting newer payloads back (V2 -> V1), composing multi-step chains
(V1 -> V2 -> V3) automatically when no direct caster is registered, preferring a registered
shortcut caster over a longer chain whenever both exist (`SchemaCastDefinitionsExpanderTest.
Expand_PrefersShortcutCasterOverLongerChain`).

Full design: `docs/specification/versioning.md`. This package is the pure casting engine
(mechanism B, §4) - CLR-type-to-CLR-type mapping and schema chain composition only. It has no
knowledge of transports, headers, or serializers; that's deliberate (§4.4) - it was originally
imported from a different project whose wire format put the schema version and topic *inside*
the JSON body, which doesn't fit Benzene's serializer-agnostic model at all. That JSON-specific
layer (`IPayloadFields`, `IPayloadSchemaVersionLookUp`, `PayloadDeserializer` reading a
`JsonElement`) has been removed entirely; what's left is unchanged, general-purpose CLR-type
mapping. The version signal now comes from the transport-neutral
`Benzene.Abstractions.MessageHandlers.Mappers.IMessageVersionGetter<TContext>` (see
`Benzene.Core.MessageHandlers/CLAUDE.md`), and the still-to-be-built request/response casting
decorators (§4.1/§4.2 - not implemented yet) will deserialize via `ISerializer`/`IPayloadSerializer`
(`Benzene.Abstractions.Serialization`), keyed by the `Type` each resolved `ISchemaCaster` already
carries - no per-format branching needed here, and no separate version-to-type registry either.

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
- `ISchemaCaster` / `SchemaCaster<TFrom, TTo>` - a caster tagged with topic + from/to schema names,
  plus the `FromType`/`ToType` CLR types (how a version-aware request/response mapper will find
  the concrete type to deserialize/serialize as, without a separate version-to-type registry)
- `SchemaCastDefinition` - topic + FromSchema + ToSchema identity
- `ISchemaCasters` / `SchemaCasters` - lookup of casters by (from, to, topic)
- `SchemaCastersBuilder` / `SchemaCasterBuilder<TFrom, TTo>` - fluent registration
- `SchemaCastDefinitionsExpander` - given the required from/to version pairs
  (`PayloadSchemaVersions`), reuses direct casters and BFS-composes multi-step chains (preferring
  a shortcut caster over a longer one when both exist - see above); throws at expansion time when
  no path exists (fail fast at startup)
- `SchemaCasterExtensions` - DI registration: `RegisterSchemaCastDefinitions` (register individual
  casters) + `RegisterPayloadSchemaVersions` (register the expanded `ISchemaCasters` singleton).
  Collects registered casters via `IServiceResolver.GetServices<ISchemaCaster>()`
- `PayloadSchemaVersions` - declares, per topic, which versions may arrive (`FromSchemas`) and
  which the registered handler(s) actually understand (`ToSchemas`)

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
- No dependency on any serializer or transport package - CLR types in, CLR types out
- Tests live in `test/Benzene.Core.Test/Core/Versioning/`
