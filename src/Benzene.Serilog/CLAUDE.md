# Benzene.Serilog

## What this package does
Serilog integration for Benzene. Adapts Benzene's logging abstractions to Serilog's ILogger interface, enabling structured logging with Serilog's rich ecosystem of sinks (Seq, Elasticsearch, Application Insights, etc.).

## Key types/interfaces

### Serilog Integration
- Adapter from `IBenzeneLogger` to Serilog `ILogger`
- Log context integration with Serilog context
- Structured logging with Serilog properties
- Enrichers for Benzene-specific data

## When to use this package
- When you prefer Serilog for logging
- For advanced structured logging scenarios
- When using Seq, Elasticsearch, or other Serilog sinks
- Popular choice for production logging

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions (logging)
- **Serilog** - Serilog logging library

## Important conventions
- Configure Serilog before Benzene startup
- Log context maps to Serilog log context
- Properties preserved for structured logging
- Enrichers add Benzene metadata
- Works with all Serilog sinks
