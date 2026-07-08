# Benzene.Diagnostics

## What this package does
Diagnostic and debugging utilities for Benzene. Provides middleware for request/response logging, performance timing, exception handling, and debugging information during development.

## Key types/interfaces

### Diagnostic Middleware
- Request/response logging middleware
- Performance timing middleware
- Exception details middleware
- Debug information middleware

## When to use this package
- During development for debugging
- For detailed request/response logging
- When troubleshooting issues
- For performance analysis
- Should be disabled or limited in production

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations

## Important conventions
- Add diagnostics middleware conditionally
- May log sensitive data - use carefully
- Performance overhead in production
- Useful for development and staging
- Can be filtered by log level
