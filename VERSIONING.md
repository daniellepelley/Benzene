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

### Single version source

All Benzene packages share one **numeric base version** (`MAJOR.MINOR.PATCH`), defined in
**`version.txt`** at the repository root. The root `Directory.Build.props` reads it into
`VersionPrefix`; individual `.csproj` files must not set `PackageVersion`/`Version`.

The publish workflow (`deploy-benzene.yml`) composes the final SemVer at pack time from the
numeric base plus a **channel** chosen as a `workflow_dispatch` input, and overrides with
`-p:PackageVersion=...`:

- `stable` → the exact base, e.g. **`1.0.0`**.
- any prerelease channel (`alpha`/`beta`/`rc`/`preview`) → the base plus an auto-incremented
  counter, e.g. **`1.0.0-rc.1`**, `1.0.0-rc.2`, … (the counter is derived from the highest
  already-published version for that base+channel on nuget.org).

To cut a new base version, change `version.txt` — nothing else. To flip from prerelease to
stable, run the workflow with the `stable` channel once the base is right (e.g. set `version.txt`
to `1.0.0` and publish `stable`).

### Packability

Projects do not pack by default (`IsPackable=false` in the root
`Directory.Build.props`). Everything under `src/` is opted in via
`src/Directory.Build.props` — including `*.TestHelpers`, `Benzene.Testing` and
`Benzene.Tools`, which are deliberately shipped as user-facing test support.
Test and example projects never pack.

### Pre-1.0

While the shared version is `< 1.0.0`, breaking changes are allowed in MINOR
versions. Once `>= 1.0.0`, breaking changes require a MAJOR bump.

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

**Note**: This versioning policy takes effect at the Benzene 1.0.0 release. Earlier alpha releases (0.x.x-alpha) did not follow strict semver.
