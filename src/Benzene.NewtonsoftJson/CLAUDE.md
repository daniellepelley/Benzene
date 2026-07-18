# Benzene.NewtonsoftJson

## What this package does
Provides a single `ISerializer` backed by Newtonsoft.Json (Json.NET), for apps that need Json.NET's
behavior instead of the default `System.Text.Json`-based serializer. It is just the serializer type -
this package ships **no** DI extension, no `IMediaFormat`, and no options object; register it yourself
wherever an `ISerializer` is required.

## Key types
- `JsonSerializer : ISerializer` (namespace `Benzene.NewtonsoftJson`) - all four members are
  `virtual`, so a subclass can override the settings.
  - Serialization uses a `CamelCasePropertyNamesContractResolver` (camelCase property names,
    matching Benzene's default serializer). `Serialize<T>` and `Serialize(Type, object)` produce the
    same output - the non-generic overload delegates to the generic one, so the `Type` argument does
    not change the result.
  - Deserialization (`Deserialize<T>` / `Deserialize(Type, string)`) uses default
    `JsonSerializerSettings` (Newtonsoft's property matching is case-insensitive).
  - Settings are constructed inline per call; there is no injectable `JsonSerializerSettings` /
    custom-converter configuration hook other than subclassing and overriding.

## When to use this package
- When you require Json.NET semantics (its converters, attributes, or nuanced type handling) rather
  than `System.Text.Json`.
- For migrating existing Json.NET-based request/response models onto Benzene.

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - `ISerializer` (`Benzene.Abstractions.Serialization`).
- **Benzene.Abstractions.Pipelines** - referenced by the project.
- **Newtonsoft.Json** (NuGet, 13.0.3) - the Json.NET engine.

## Important conventions
- You register `JsonSerializer` yourself (this package has no `AddNewtonsoftJson`/`UseNewtonsoftJson`
  extension); it is a drop-in `ISerializer` replacement.
- Serialized output is camelCase by default; override the `virtual` members to change contract
  resolvers, converters, or other settings.
