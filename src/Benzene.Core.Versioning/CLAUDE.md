# Benzene.Core.Versioning

## What this package does
Payload multi-versioning support: casts a payload from the schema version it was published with
to the schema version the consuming message handlers understand - upcasting older payloads
forward (V1 -> V2) or downcasting newer payloads back (V2 -> V1), composing multi-step chains
(V1 -> V2 -> V3) automatically when no direct caster is registered, preferring a registered
shortcut caster over a longer chain whenever both exist (`SchemaCastDefinitionsExpanderTest.
Expand_PrefersShortcutCasterOverLongerChain`).

Full design: `docs/specification/versioning.md` (mechanism B, §4). The core is a pure casting
engine (CLR-type-to-CLR-type mapping + schema chain composition); on top of that sit the
transport-facing request/response decorators that wire it into the message pipeline.

The engine has no knowledge of the JSON-in-body assumptions it was imported with - that JSON layer
(`IPayloadFields`, `IPayloadSchemaVersionLookUp`, `PayloadDeserializer` reading a `JsonElement`) was
removed in Phase 2 (§4.4). The version signal now comes from the transport-neutral
`Benzene.Abstractions.MessageHandlers.Mappers.IMessageVersionGetter<TContext>` (see
`Benzene.Core.MessageHandlers/CLAUDE.md`), and deserialization goes through the negotiated
`ISerializer`/`IPayloadSerializer` via the decorators below - so casting works for JSON, XML,
MessagePack, and Avro alike, keyed by the `Type` each `ISchemaCaster` carries (no per-format
branching, no separate version-to-type registry).

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
  plus the `FromType`/`ToType` CLR types and a non-generic `Cast(object)` (so a caller that only
  knows runtime types - the decorators below - invokes the typed caster without reflection)
- `SchemaCastDefinition` - topic + FromSchema + ToSchema identity
- `ISchemaCasters` / `SchemaCasters` - lookup of casters. `GetSchemaCaster(from, to, topic)` (both
  versions as strings, throws - used by chain expansion at startup) plus two O(1) `TryGetSchemaCaster`
  overloads keyed by one version string + one CLR `Type` (used per-message by the decorators):
  `(topic, fromSchema, toType)` for request upcast, `(topic, fromType, toSchema)` for response
  downcast. The type-keyed match is why "multiple canonical versions per topic" needs no special rule

### Request/response casting decorators + DI (`Request/`, `Response/`, root)
- `CastingRequestMapper<TContext>` - decorates the transport's `IRequestMapper<TContext>`: reads the
  incoming version + topic, finds the `(topic, version -> TRequest)` caster, has the inner mapper
  deserialize the body as the caster's `FromType` (so the negotiated serializer/byte path still
  apply - via the compiled-delegate cache `RequestBodyReader<TContext>`), then upcasts to `TRequest`.
  Delegates straight through when unversioned/no-topic/no-caster - zero overhead for opted-out topics
- `CastingResponsePayloadMapper<TContext>` - decorates `IResponsePayloadMapper<TContext>`: downcasts
  the canonical response payload to the requested version (symmetric by default - same version the
  request declared) and hands the inner mapper a shim result (`CastMessageHandlerResult` +
  `ResponseTypeOverrideDefinition`) so serialization/raw-content handling is reused, not
  reimplemented. Passes through for failures, no version, null/raw-string payloads, or no caster
- `PayloadVersionCastingExtensions.UsePayloadVersionCasting<TContext>()` - opt-in DI wiring: wraps
  that context's default request/response mappers with the decorators. **Call after the transport's
  own registration** so the closed decorator registrations win. **Register both cast directions** -
  an upcast V1->V2 does not give the response downcast V2->V1; the simplest way is to list every live
  version in *both* `FromSchemas` and `ToSchemas` of the `PayloadSchemaVersions` you pass to
  `RegisterPayloadSchemaVersions`, so the expander composes every pair. Wraps the framework-default
  mappers only - a bespoke request mapper (e.g. gRPC's) is not wrapped on the request side
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
- **Benzene.Abstractions** - DI + serialization abstractions
- **Benzene.Abstractions.MessageHandlers** - the `IRequestMapper`/`IResponsePayloadMapper`/
  `IMessageVersionGetter`/`IMessageTopicGetter` interfaces the decorators implement/consume
- **Benzene.Core.MessageHandlers** - the concrete default mappers the decorators wrap
  (`MultiSerializerOptionsRequestMapper`/`DefaultResponsePayloadMapper`). This package is the opt-in
  leaf: nothing references it unless it wants casting, so the dependency points *up* the stack from
  here, never the reverse (Core.MessageHandlers has no knowledge of versioning)

## Important conventions
- Schema versions are plain strings ("V1", "V2"...) scoped per topic
- Casting is configured at startup and compiled once; missing paths fail at expansion, not
  per message. Register both cast directions for symmetric request-up/response-down (see the
  decorators section above)
- The pure engine (`Casters/`, `CasterBuilder/`, `Schemas/` minus the decorators) is
  serializer/transport-agnostic; only the decorators touch `ISerializer`/the message pipeline
- Tests live in `test/Benzene.Core.Test/Core/Versioning/`
