# Benzene.Xml

## What this package does
XML serialization integration for Benzene. Provides ISerializer implementation using System.Xml, enabling XML request/response handling for legacy systems or XML-based APIs.

## Key types/interfaces

### XML Integration
- `ISerializer` implementation for XML
- XmlSerializer configuration
- XML serialization options

## When to use this package
- When working with XML-based APIs
- For SOAP-like services
- When integrating with legacy XML systems
- For applications requiring XML format

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions (serialization)
- **System.Xml** - .NET XML serialization

## Important conventions
- Register as ISerializer in DI
- XML attributes on request/response classes
- Works with Benzene serialization infrastructure
- Can be used alongside JSON serializers
