# Benzene.Results

## What this package does
Concrete result types and helpers for Benzene message handlers. Handlers return an
`IBenzeneResult`/`IBenzeneResult<T>` (the interfaces live in `Benzene.Abstractions.Results`) built
through this package's `BenzeneResult` factory, so success/failure is modelled explicitly with a
status string rather than thrown exceptions. Transport adapters map the status to their own codes
(HTTP status, etc.).

## Key types/interfaces
- `BenzeneResult` - static factory for building results. Named helpers (`Ok`, `Created`, `Accepted`,
  `Updated`, `Deleted`, `Ignored`, `NotFound`, `BadRequest`, `ValidationError`, `Conflict`,
  `Forbidden`, `Unauthorized`, `ServiceUnavailable`, `NotImplemented`, `UnexpectedError`,
  `TooManyRequests`, `Timeout`), plus low-level `Set(...)` overloads including
  `Set<T>(status, payload, isSuccessful)` for the case where the success class must not be derived
  from the status (e.g. a health check reporting `ServiceUnavailable` while staying successful so
  the body renders the report).
- `BenzeneResultStatus` - the framework-defined status-string vocabulary (`const string` values)
  plus classifiers `IsSuccess`, `IsFailure`, `IsKnown`, `IsTransient`. Success is derived from the
  status class: a known failure status yields an unsuccessful result even with a payload; unknown
  application-defined statuses default to successful.
- `BenzeneResultExtensions` - `IsOk`/`IsNotFound`/`IsTransient`/... status predicates, `As<...>`
  mapping/projection helpers (sync and `Task`-returning), `AsTask`, and `HttpStatusCode.Convert(...)`
  to/from Benzene statuses.
- `ProblemDetails` / `ErrorPayload` - RFC-7807-style error body shapes used when rendering a
  failure result's errors.

Note: `IBenzeneResult` and `IBenzeneResult<T>` themselves (and `Void`) are declared in
`Benzene.Abstractions.Results`, not here. This package supplies the concrete builders and helpers.

## When to use this package
- Anywhere a handler needs to return a result — use `BenzeneResult.Ok(...)` / `.NotFound(...)` etc.
  instead of throwing for expected outcomes.
- When mapping between HTTP status codes and Benzene statuses.
- When inspecting a result's status class (success/failure/transient) in middleware.

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - for `IBenzeneResult`, `IBenzeneResult<T>`, and `Void`.

## Important conventions
- Return `IBenzeneResult`/`IBenzeneResult<T>` from handlers instead of throwing for expected outcomes.
- Status strings are the case-sensitive wire vocabulary — use the `BenzeneResultStatus` constants.
- `IsSuccessful` is derived from the status class unless an explicit `isSuccessful` overload is used.
- `IsTransient` marks retry-*eligible* statuses only; it is not a retry-*safety* guarantee (a
  `Timeout` leaves the operation's application state unknown — see `BenzeneResultStatus.IsTransient`).

## Tests
Covered by `test/Benzene.Core.Test` (result construction, status classification, and
`HttpStatusCode` conversion).
