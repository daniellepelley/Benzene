# Benzene.Log4Net

## What this package does
Log4Net integration for Benzene. Adapts Benzene's logging abstractions to Log4Net's ILog interface, enabling integration with Log4Net appenders and legacy logging infrastructure.

## Key types/interfaces

### Log4Net Integration
- Adapter from `IBenzeneLogger` to Log4Net `ILog`
- Log context integration with Log4Net context
- Log level mapping
- MDC/NDC support

## When to use this package
- When working with legacy Log4Net infrastructure
- For enterprise environments standardized on Log4Net
- When migrating from Log4Net-based systems

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions (logging)
- **log4net** - Log4Net logging library

## Important conventions
- Configure Log4Net before Benzene startup
- Log context maps to Log4Net MDC
- Log levels mapped to Log4Net levels
- Works with all Log4Net appenders
