# Versioning Policy

Benzene follows [Semantic Versioning 2.0.0](https://semver.org/).

## Version Format

Versions are expressed as `MAJOR.MINOR.PATCH` (e.g., `1.2.3`).

### MAJOR version

Incremented when making **incompatible API changes**:
- Removing public types, methods, or properties
- Changing method signatures
- Changing behavior in breaking ways
- Removing or renaming packages

### MINOR version

Incremented when adding **backwards-compatible functionality**:
- Adding new public types, methods, or properties
- Adding new packages
- Adding new features that don't break existing code
- Deprecating functionality (without removing it)

### PATCH version

Incremented for **backwards-compatible bug fixes**:
- Fixing bugs without changing public API
- Performance improvements
- Documentation updates
- Internal refactoring

## Pre-release Versions

Pre-release versions use suffixes:
- `1.0.0-alpha.1` - Early testing, may have significant bugs
- `1.0.0-beta.1` - Feature complete, stabilizing
- `1.0.0-rc.1` - Release candidate, final testing
- `1.0.0-preview.1` - Preview of upcoming features

## Target Framework Support

### Current Support
- **.NET 10** - Primary target

### Support Policy
- Benzene supports the current .NET LTS (Long-Term Support) release and the latest .NET release
- Support for older frameworks is dropped in MAJOR versions only
- Target framework changes are documented in CHANGELOG.md

## Deprecation Policy

When deprecating functionality:

1. **Mark as Obsolete**: Add `[Obsolete("Reason", false)]` attribute
2. **Document**: Update CHANGELOG.md with deprecation notice
3. **Provide Alternative**: Document the replacement in XML docs
4. **Wait Period**: Minimum one MINOR version before removal
5. **Remove**: Remove in next MAJOR version

Example:
```csharp
/// <summary>
/// Does something (DEPRECATED: Use NewMethod instead)
/// </summary>
/// <remarks>
/// This method will be removed in version 2.0.
/// Use <see cref="NewMethod"/> instead.
/// </remarks>
[Obsolete("Use NewMethod instead. This will be removed in v2.0.", false)]
public void OldMethod() { }
```

## Package Versioning Strategy

### Core Packages (Stable at 1.0+)
Packages at 1.0.0 or higher follow strict semver:
- `Benzene.Abstractions`
- `Benzene.Abstractions.Middleware`
- `Benzene.Core`
- `Benzene.Core.Middleware`
- `Benzene.Http`

### Preview Packages
Some packages may remain at `0.x.x` or use `-preview` suffix while maturing:
- Breaking changes allowed in MINOR versions while `< 1.0.0`
- Breaking changes in MAJOR versions once `>= 1.0.0`

Check individual package versions on NuGet.

## Compatibility Guarantees

### What We Guarantee
- **Public API stability**: No breaking changes to public types/methods within same MAJOR version
- **Binary compatibility**: Assemblies with same MAJOR version are binary compatible
- **Behavioral compatibility**: Existing functionality works the same way

### What We Don't Guarantee
- **Internal implementations**: Internal types may change in any version
- **Undocumented behavior**: Relying on undocumented behavior may break
- **Performance characteristics**: Performance may improve/degrade without being a breaking change
- **Dependency versions**: Third-party dependency versions may change in MINOR versions

## Dependency Management

### NuGet Dependencies
- **MAJOR** changes: May update dependencies to new major versions
- **MINOR** changes: May update dependencies to new minor versions
- **PATCH** changes: May update dependencies for critical security fixes only

### Benzene Package Dependencies
All Benzene packages with the same MAJOR version are compatible with each other.

Example:
- ✅ `Benzene.Core` 1.2.0 works with `Benzene.Aws.Lambda.Core` 1.5.3
- ❌ `Benzene.Core` 2.0.0 may NOT work with `Benzene.Aws.Lambda.Core` 1.5.3

## Long-Term Support (LTS)

### LTS Versions
Major versions designated as LTS receive:
- Critical security fixes for 2 years
- Critical bug fixes for 1 year
- LTS designation announced with release

### Non-LTS Versions
- Supported until next MAJOR version is released
- Receive bug fixes and security patches until then

## Questions?

For questions about versioning or compatibility:
- Check CHANGELOG.md for specific changes
- Open an issue on GitHub
- See migration guides in `docs/` folder (when available)

---

**Note**: This versioning policy took effect with Benzene 1.0.0. Earlier alpha releases (0.x.x-alpha) did not follow strict semver.
