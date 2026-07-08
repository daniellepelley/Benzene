# Benzene.NewtonsoftJson

## What this package does
Newtonsoft.Json (Json.NET) serialization integration for Benzene. Provides ISerializer implementation using Newtonsoft.Json, enabling Json.NET's rich feature set (custom converters, contract resolvers, etc.).

## Key types/interfaces

### Newtonsoft.Json Integration
- `ISerializer` implementation using Json.NET
- Custom JsonSerializerSettings support
- Converters and contract resolvers
- Backward compatibility with existing Json.NET code

## When to use this package
- When you need Json.NET-specific features
- For backward compatibility with existing code
- When migrating from Json.NET to Benzene
- Alternative to System.Text.Json

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions (serialization)
- **Newtonsoft.Json** - Json.NET library

## Important conventions
- Register as ISerializer in DI
- Configure JsonSerializerSettings as needed
- Works with all Benzene serialization points
- Can coexist with System.Text.Json via context
