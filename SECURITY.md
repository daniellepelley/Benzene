# Security Policy

## Supported versions

Benzene is pre-1.0. Security fixes are made against the latest release and `main`. Once 1.0 ships,
this section will list the supported release lines.

| Version | Supported |
|---------|-----------|
| latest prerelease / `main` | ✅ |
| older prereleases | ❌ |

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues, discussions, or
pull requests.**

Instead, use GitHub's private vulnerability reporting:

1. Go to the repository's **Security** tab → **Report a vulnerability**
   (<https://github.com/daniellepelley/Benzene/security/advisories/new>).
2. Describe the issue, the affected package(s) and version(s), and — if you can — a minimal
   reproduction and the impact.

This opens a private advisory visible only to you and the maintainers.

## What to expect

- We aim to acknowledge a report within a few days.
- We'll work with you to confirm the issue, assess impact, and prepare a fix.
- Once a fix is released, we'll credit you in the advisory (unless you'd prefer to remain
  anonymous).

## Scope

Benzene is a library, not a hosted service. The most relevant classes of report are: issues in
Benzene's own code that could let untrusted input compromise a host (deserialization, auth/token
handling in `Benzene.Auth.*`, header/routing handling), and unsafe defaults. Vulnerabilities in
the underlying cloud SDKs or the .NET runtime should be reported to those projects directly.
