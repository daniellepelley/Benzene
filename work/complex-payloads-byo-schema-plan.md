# Complex payload models & bring-your-own schema documents

**Status:** phases 1–3 implemented; phase 4 implemented except 4.4 (deferred — spec-gated);
phase 5 items 1–2 done (5.2 resolved by documentation), 5.3 backlog.

## Motivation

Teams adopting Benzene sometimes have payload models that are not simple flat DTOs: deep
inheritance hierarchies, polymorphic members (base-typed properties holding derived instances),
generic wrappers (`Envelope<T>`), and — crucially — **pre-existing schema documents** that are the
contractual source of truth, which they want Benzene to *serve*, not regenerate by reflection.
The question this plan answers: does supporting that need a fundamental rework, or targeted work?

**Answer: targeted work.** The runtime already handles most of it; the contract side (spec,
schema generation, downstream consumers) needed a DI seam and configuration surface, which this
plan adds in phases.

## Research findings (July 2026)

### Runtime payload path — mostly works today
- Deep inheritance and **closed** generic request/response types work end to end: discovery
  matches `IMessageHandler<Envelope<Foo>, Result<Bar>>`, nothing keys on short type names
  (dedup by `AssemblyQualifiedName`, routing by topic), enrichment reflects inherited properties.
- Polymorphic (de)serialization works when models carry System.Text.Json's
  `[JsonPolymorphic]`/`[JsonDerivedType]` attributes — Benzene's `JsonSerializer` uses bare STJ
  options and honors model attributes. Unannotated base-typed *nested* members silently drop
  derived fields (standard STJ behavior).
- Gotcha: the negotiated JSON path resolves the **concrete** `JsonSerializer`
  (`JsonMediaFormat`), not `ISerializer` — a global custom-options serializer must be registered
  as the concrete `JsonSerializer` *before* `AddBenzene()`, or plugged as an
  `IMediaFormat<TContext>` (see `Benzene.Xml` for the pattern).
- Open-generic handler classes (`FooHandler<T>` for many `T`) are unsupported
  (`MessageHandlerFactory` needs closed types; `[Message]` carries one topic). Separable feature;
  out of scope here.

### Contract path — where the gaps were
- Three independent reflection generators, all property-flattening, none polymorphism-aware:
  Swashbuckle in `Benzene.Schema.OpenApi` (bare `SchemaGeneratorOptions`), JsonSchema.Net in
  `Benzene.JsonSchema`, and the hand-rolled `MeshSchemaGenerator` in `Benzene.Mesh.Wire`.
  Derived-only fields vanish from published contracts silently.
- `SpecBuilder.CreateSchemaBuilder` hardcoded `new SchemaBuilder()` — `ISchemaBuilder` existed
  (with an `AddSchema(id, OpenApiSchema)` injection method) but was unreachable from DI, so a
  running service could not substitute its own schema source into `/benzene/spec`.
- `Benzene.JsonSchema`'s `IJsonSchemaProvider<TContext>` was already a genuine BYO seam for
  *validation* — but nothing tied it to the *published* schema, so contract and validation could
  drift.
- Downstream consumers mangle composition keywords: the AsyncAPI mapper copies a fixed field
  allowlist (drops `oneOf`/`allOf`/`discriminator`); the codegen client emits flat classes; the
  mesh aggregator's `$ref` inliner doesn't recurse into `oneOf`/`allOf` branches. The mesh
  *catalog* itself is schema-agnostic (each service's spec flows through verbatim as
  `MeshServiceSnapshot.SpecJson`).

## Phases

### Phase 1 — DI seam for the spec's schema builder ✅ (this change)
`SpecBuilder.CreateSchemaBuilder` now try-resolves `ISchemaBuilder` from DI before falling back
to `new SchemaBuilder(...)`. The `IValidationSchemaBuilder` decorator still wraps whichever
builder is chosen. Register custom builders **transient** (or scoped): a schema builder
accumulates one document's components catalogue, so instances must not be shared across builds.

### Phase 2 — bring-your-own schema documents ✅ (this change)
- `Benzene.Schema.OpenApi`:
  - `SuppliedSchemaCatalog` — immutable-after-setup registry of hand-authored schemas
    (`schemaId → OpenApiSchema`) plus `Type → schemaId` mappings. Load programmatically, from
    per-schema JSON (`AddJson`), or from a `components.schemas`-shaped JSON object
    (`AddComponentsJson`).
  - `SuppliedSchemaBuilder : ISchemaBuilder` — serves catalog schemas for mapped types (returning
    `$ref`s) and falls back to reflection (`SchemaBuilder`) for everything else. On the first
    mapped hit it registers the **whole catalog** into the components section so cross-`$ref`s
    between hand-authored schemas resolve.
  - `Extensions.AddSuppliedSchemas(catalog)` — one-call DI wiring (transient `ISchemaBuilder`
    over the singleton catalog, honoring `SchemaGenerationOptions` for the fallback).
- `Benzene.JsonSchema`:
  - `SuppliedJsonSchemaCatalog` (+ `AddJson` via JsonSchema.Net) and
    `SuppliedJsonSchemaProvider<TContext>` — validation uses the hand-authored schema for mapped
    request types and falls back to the generated one otherwise.
  - `AddSuppliedJsonSchemas(catalog)`; `AddJsonSchema()`'s provider registration switched to
    `TryAddScoped` so a user/supplied registration made in `ConfigureServices` reliably wins
    (previously double-registered via plain `AddScoped`).
- Feed both catalogs from the same schema documents to keep the published contract and runtime
  validation aligned. (A single shared store across both packages was deliberately not
  introduced — the packages stay independent; revisit if drift proves a problem in practice.)

### Phase 3 — opt-in inheritance/polymorphism in generated schemas ✅ (this change)
- `SchemaGenerationOptions` (`Benzene.Schema.OpenApi`) with `UseAllOfForInheritance` and
  `UseOneOfForPolymorphism`, applied to Swashbuckle's generator by `SchemaBuilder` (which now
  takes optional options; `SpecBuilder` resolves them from DI; registered via
  `Extensions.SetSchemaGenerationOptions`).
- Subtype/discriminator resolution defaults to the model's own STJ attributes
  (`[JsonDerivedType]` for subtypes and discriminator values, `[JsonPolymorphic]` for the
  discriminator property name, `$type` when unspecified) via `JsonPolymorphism`, so the emitted
  contract matches what the runtime serializer actually does. Custom resolvers can be supplied
  for unannotated hierarchies.
- Off by default — existing specs are byte-identical unless options are registered.

### Phase 4 — teach the consumers composition keywords
1. ✅ AsyncAPI `Mapper.Map` carries `oneOf`/`allOf`/`anyOf` (branches mapped recursively, refs
   preserved), `discriminator` (property name — AsyncAPI's schema dialect models it as a string),
   and `additionalProperties`.
2. ✅ Mesh aggregator `InlineSchema` recurses into `oneOf`/`allOf`/`anyOf` branches when inlining
   `$ref`s, so a polymorphic topic schema lands in `topics.json` fully self-contained.
3. ✅ CodeGen client: `allOf` [base `$ref` + own props] generates `class X : Base` with only the
   own properties; a schema with a `discriminator` + mapping gets STJ
   `[JsonPolymorphic]`/`[JsonDerivedType]` attributes (and no POCO property for the discriminator
   itself, which is serializer metadata); a `oneOf`-union member site is typed as the subtypes'
   shared `allOf` base when the components catalogue reveals one, else `object`.
4. ⏸ `MeshSchemaGenerator` (Mesh.Wire): **deferred — spec-gated.** The §2.1 CLR→schema mapping is
   pinned by `docs/specification/mesh.md` + the language-neutral conformance fixtures (+ the Go
   reference implementation), and its subset deliberately contains no `oneOf`. Emitting `oneOf`
   for `[JsonDerivedType]` members would produce non-conformant descriptors and shift
   `descriptorHash`es. Requires a spec revision (fixtures + reference implementation updated
   alongside) first; until then annotated base-typed members keep degrading to the base's schema.

### Phase 5 — hygiene
- ✅ `OpenApiValidationSchemaBuilder` now looks the catalogued schema up by the id the inner
  builder actually returned (`output.Reference.Id`) instead of raw `type.Name`, so validation
  facets decorate generic wrappers (`MessageWrapper<Foo>` → `FooMessageWrapper`) too.
- ✅ Serializer-replacement gotcha: documented in `Benzene.Core.MessageHandlers/CLAUDE.md`
  ("Important conventions") — replacing `ISerializer` alone doesn't reach the negotiated JSON
  path; register the concrete `JsonSerializer` (with custom options) before `AddBenzene()`, or a
  custom `IMediaFormat`. Rebinding `JsonMediaFormat` to `ISerializer` was considered and
  deliberately not done: it changes a public ctor and would silently redirect the
  `application/json` format to whatever `ISerializer` happens to be registered.
- Backlog: consider a startup diagnostic when `[JsonPolymorphic]` models are served without
  `UseOneOfForPolymorphism` (contract silently narrower than runtime behavior).
