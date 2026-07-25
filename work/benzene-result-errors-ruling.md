# `BenzeneResult.Errors` — the structured-error model — RULING

**Status:** ✅ **APPROVED (with amendments)** — core product owner ruling, 2026-07-25. Answers the
open question in `work/error-payload-proposal.md` §5.1, which gates the rest of the error-payload
work (`work/spec-review-2026-07-25.md` §2, task #28). Decision only; the implementation is a
separate, scoped task.
**Last Updated:** 2026-07-25
**Purpose:** Settle the result-model question — *does `IBenzeneResult.Errors` become structured, and
does that land before the 1.0 tag* — with the blast radius measured rather than asserted, so the
wire-shape, spec and client work downstream of it can proceed.

---

## The ruling in three lines

1. **Shape: option (a) in intent, option (c) in mechanism.** `IBenzeneResult.Errors` becomes
   `IReadOnlyList<BenzeneError>`. The structured list is primary *and keeps the name `Errors`*. The
   string projection survives as an extension method, not as a second interface member.
2. **Scope: in scope for 1.0. It lands before the tag.** Deferring does not defer the decision —
   it makes it, permanently, and makes the worst one.
3. **Blast radius, measured: 8 statements in `src/`, 23 assertions in `test/`, 0 live sites in
   `examples/`, `templates/`, `deploy/`, `benchmarks/`, 1 prose mention in `docs/`.** All 324
   result-construction sites are unaffected.

---

## 1. User & job

Two users, one job each, and today the framework serves neither.

**The API caller** (a browser form, a partner service, a mobile client) receives
`{"status":"validation-error","detail":"Name must not be empty, Age must be greater than 0"}` and
wants to put a red message next to the *Name* input. Its job is *render this failure against the
field that caused it*. It cannot: the field name was destroyed inside the framework before the
response was ever built, and splitting `detail` on `", "` is unsafe by construction (validation
messages contain commas).

**The calling service** wants to branch — retry on one rule, escalate on another. Its job is
*decide, mechanically, what kind of failure this was*. It cannot: there is no machine-readable code,
only prose the application author is free to reword at any time, which makes any `Contains("must not
be empty")` check a latent breakage.

There is a third, quieter user: **Benzene itself**. `src/Benzene.Grpc/GrpcMethodHandler.cs` already
packs a `google.rpc.BadRequest` into the `grpc-status-details-bin` trailer, one
`FieldViolation` per error — and can only fill in `Description`, leaving `FieldViolation.Field`
empty, because the model has nothing to put there. We are already shipping a degraded structured
error surface to a structured-error-capable transport. That is the clearest evidence that the model,
not the wire format, is the constraint.

## 2. Business value

- **This is the difference between an error payload a client can act on and one it can only
  display.** Benzene has first-class validation across three integrations; "we know exactly which
  field failed and we throw it away" is a hard-to-defend gap in a framework that markets validation
  as built-in.
- **It is worth roughly a day now and a major version later.** There is no additive path: the type
  of an existing interface member cannot change in a minor release. Post-1.0 the only options are
  (i) 2.0, or (ii) bolt a second member on and carry two error collections for the life of 1.x.
- **It unblocks four other pieces of work** — the wire `errors` array, the client round-trip fix,
  the gRPC `FieldViolation.Field` fill-in, and the spec §1.3 rewrite — each of which is currently
  blocked on or diminished by this one decision.
- **The cost is borne entirely by us, once.** Every affected call site is in this repository, is
  caught by the compiler, and is mechanical.

## 3. Technical assessment

### 3.1 The abstraction must change — that is the crux, and it settles where the type lives

`IBenzeneResult.Errors` is declared in `src/Benzene.Abstractions/Results/IBenzeneResult.cs` (the
`Benzene.Abstractions.Results` *namespace* inside the `Benzene.Abstractions` *assembly* — there is no
separate `Benzene.Abstractions.Results` package). Every framework consumer of errors reads them
through the interface, not through the concrete type:
`DefaultResponsePayloadMapper` builds the `ErrorPayload` from an `IBenzeneResult`;
`MessageRouter` logs from an `IBenzeneResult`; `GrpcMethodHandler` reads
`MessageHandlerResult.BenzeneResult.Errors`.

Therefore: **structured errors that the pipeline can serialize require the interface to expose them.
There is no version of this change that leaves `Benzene.Abstractions` untouched** — short of a
capability probe (`if (result is IHasErrorDetails …)`), which buys nothing here and costs a
permanent, discoverable-by-accident second interface.

Consequently **`BenzeneError` belongs in `Benzene.Abstractions` (namespace
`Benzene.Abstractions.Results`), not `Benzene.Results`.** An abstraction cannot be typed in terms of a
type it does not own. This adds **zero** dependencies: it is a POCO, and `Benzene.Abstractions`
targets `net10.0` only.

`IBenzeneResult<T>` itself is **not** modified. Only the inherited `Errors` member changes type;
`Payload` and the generic contract are untouched.

### 3.2 The shape

```csharp
namespace Benzene.Abstractions.Results;

/// <summary>One error in a failed result: the message, and optionally the field it belongs to
/// and a machine-readable code a caller can branch on.</summary>
public sealed record BenzeneError
{
    public BenzeneError() { }
    public BenzeneError(string message, string? field = null, string? code = null) { … }

    public string Message { get; init; } = string.Empty;
    public string? Field { get; init; }
    public string? Code { get; init; }

    public override string ToString() => Message;
}
```

```csharp
public interface IBenzeneResult
{
    string Status { get; }
    bool IsSuccessful { get; }
    object PayloadAsObject { get; }
    IReadOnlyList<BenzeneError> Errors { get; }   // was string[]
}
```

Four deliberate choices, each load-bearing:

- **`record`, with value equality.** Test ergonomics — `Assert.Equal(new BenzeneError("Name must not
  be empty", "Name", "NotEmptyValidator"), result.Errors[0])` is the assertion we want people to
  write, and `Benzene.Testing` being delightful is part of this package family's remit.
- **A public parameterless constructor *and* init-only properties, not a positional record.** The
  same type is the wire DTO (`ErrorPayload.Errors`), and Benzene ships five serializers
  (`System.Text.Json` default, Newtonsoft, Xml, MessagePack, Avro). A positional record has no
  parameterless constructor and would break the ones that require one. Init-only setters are
  ordinary setters to reflection, so this shape round-trips everywhere.
- **`ToString() => Message`.** This is the compatibility softener that matters:
  `string.Join("; ", result.Errors)` — the exact expression at
  `src/Benzene.Core.MessageHandlers/MessageRouter.cs:142` and
  `src/Benzene.Grpc/GrpcMethodHandler.cs:122` — compiles unchanged and produces byte-identical
  output, because `string.Join` is generic over `T` and calls `ToString()`.
- **`IReadOnlyList<BenzeneError>`, not `BenzeneError[]`.** Ordering is normative in the proposed wire
  shape, and `IReadOnlyList<T>` preserves it while closing a real defect: today `Errors` hands back
  the internal array, so any caller can mutate a result's errors in place. Empty stays empty, never
  null, backed by a shared empty instance so the success path allocates nothing.

The string projection lives in `Benzene.Results`, as an extension, **not on the interface**:

```csharp
public static string[] ErrorMessages(this IBenzeneResult result);   // BenzeneResultExtensions
```

A value derivable from the primary data does not earn a slot on an abstraction that every external
implementer must satisfy.

### 3.3 Construction stays 100% source-compatible

All **324** `BenzeneResult.*` construction sites (125 in `src/`, 181 in `test/`, 18 in `examples/`)
keep compiling untouched: every `params string[] errors` factory overload is retained and simply
projects each string to `new BenzeneError(message)`.

The structured overloads are added as **non-`params` `IReadOnlyList<BenzeneError>` overloads**, and
for 1.0 **only to `Set`/`Set<T>`, `ValidationError`/`ValidationError<T>` and
`BadRequest`/`BadRequest<T>`** — the general escape hatch plus the two statuses the validation
integrations actually produce. Two reasons: `params BenzeneError[]` would make the existing zero-arg
calls (`BenzeneResult.NotFound()`) ambiguous and break them; and overloads are *additive*, so the
remaining twelve factories can grow structured variants any time in 1.x without a major bump. Freeze
the thing that cannot be added later; leave the convenience surface room to grow.

### 3.4 Options considered and rejected

| | Option | Verdict |
|---|---|---|
| **(a′)** | **Structured primary, keeps the name `Errors`; string projection as an extension method** | ✅ **Adopted** |
| (a-literal) | Structured primary under a new name (`ErrorDetails`), `string[] Errors` retained as a derived property | Rejected — see below |
| (b) | `string[] Errors` primary + parallel structured collection | Rejected — two sources of truth, and the *lossy* one stays canonical. Agreed with the proposal's own assessment. |
| (c) | Change `Errors` outright, let call sites break | This *is* the adopted mechanism; (a′) is (c) plus the softeners in §3.2–3.3 that reduce the break to 31 mechanical read sites and zero construction sites. |
| (d) | Defer to post-1.0, wire-only fallback | Rejected — see §4. |
| (e) | Capability interface `IHasErrorDetails`, probed by the payload mapper | Rejected — precedent exists (`IPayloadSerializer : ISerializer`), but that one is a *performance* opt-in where both paths are correct. Here one path is lossy, so a probe institutionalises the defect and adds a permanently public interface to dodge editing 31 lines we own. |

**Why (a-literal) is rejected, specifically** — because it is the tempting one. It is fully
source-compatible, and with a C# default interface member (`string[] Errors =>
ErrorDetails.Select(e => e.Message).ToArray();`) it can even avoid burdening implementers. But:

- It permanently makes the **lossy** member the well-named, IntelliSense-first, tutorial-default one.
  Every new user types `.Errors`, gets strings, and never learns the structured data exists. That is
  an anti-pit-of-success API frozen for the whole 1.x line — and pit-of-success design is precisely
  what this package family exists to protect.
- A DIM projection is a *convention*, not a guarantee: any implementer may override `Errors` and
  desynchronise it from `ErrorDetails`. The "single source of truth" is unenforced.
- Computed on every access, it allocates a `Select().ToArray()` per read. `MessageRouter` reads
  `Errors` twice per failed message. Core-pipeline allocation on the failure path is my remit.

We would be buying 31 lines of one-time, compiler-verified edits in a repo we control, at the price
of a permanent inversion of the API's discoverability. Pre-1.0 is exactly when you refuse that trade.

### 3.5 Source vs binary compatibility

- **Source:** breaking for *readers* of `.Errors` (31 sites in-repo, all mechanical) and for anyone
  implementing `IBenzeneResult` outside the repo. Non-breaking for all constructors of results,
  which is where the overwhelming majority of user code lives.
- **Binary:** breaking for everyone. Changing the type of an interface member changes its signature;
  every consuming assembly must be recompiled. There is no partial mitigation and none is worth
  attempting.
- **Semver:** no promise is broken. `version.txt` is `0.0.2`, `git tag` is empty, and every published
  package is an alpha prerelease. This is exactly the window the freeze reserves for this kind of
  call.

## 4. Is it in scope for 1.0? — Yes. Measured, not asserted.

### 4.1 The freeze binds at the tag, not at the proposal

`work/1.0-api-freeze-proposal.md` is marked EXECUTED on 2026-07-18. The objection — "the freeze
already happened, this arrives after it" — does not survive contact with the record:

- **2026-07-21**, three days *after* that document was marked executed,
  `work/api-shape-proposal-1.0.md` records option **1c**: the legacy `IMessageResult` interface and
  `MessageResult` class **deleted outright**, described in that document as "a hard breaking change,
  acceptable pre-1.0", migrating ~19 context types and ~23 test files.
- The same freeze pass itself renamed a method on the dispatch interface (`HandlerAsync` →
  `HandleAsync`) and a public type family across ~19 transport packages.

So the established, exercised standard is: **breaking changes to core abstractions are in scope
until the tag, provided they are justified, measured, and land behind a green build.** That is the
bar this change must clear — not "the freeze document exists, therefore no."

### 4.2 The blast radius, counted

| Surface | Read sites of `IBenzeneResult.Errors` | Notes |
|---|---|---|
| `src/` | **8 statements, 6 files** | `Core.Versioning/Response/CastMessageHandlerResult.cs:39`; `Grpc/GrpcMethodHandler.cs:121` (+ the `string[]? errors` parameter and loop in `AddRichErrorDetails`); `Results/BenzeneResultExtensions.cs:112,120`; `Clients.HealthChecks/ClientHealthCheck.cs:69,71`; `Core.MessageHandlers/MessageRouter.cs:142`; `Core.MessageHandlers/Response/DefaultResponsePayloadMapper.cs:43` |
| `test/` | **23 assertions, 11 files** | Heaviest is `Clients/Aws/Lambda/LambdaResultExtensionTest.cs` (8) |
| `examples/` | **0 live** | 4 textual hits, every one inside commented-out code |
| `templates/` | **0** | |
| `deploy/` | **0** | |
| `benchmarks/` | **0** | |
| `docs/` | **1** | `docs/getting-started-grpc.md:339` (prose). The hit in `docs/cookbooks/fluentvalidation-custom-rules.md:397` is FluentValidation's own `ValidationResult.Errors`, not ours |

Two of the eight `src/` sites (`MessageRouter`, `GrpcMethodHandler`'s `string.Join`) compile
unchanged thanks to `ToString()`; two more are `.Length` → `.Count`.

**Implementers of `IBenzeneResult` in the repo: 2, both `private` nested classes** —
`BenzeneResult.ServiceBenzeneResultInternal<T>` and
`CastMessageHandlerResult.CastBenzeneResult` (`src/Benzene.Core.Versioning`). Neither is nameable
from outside its file. `Benzene.Cache.Core` has 6 `where TResult : IBenzeneResult` generic
constraints — consumers, not implementers; unaffected.

**Construction sites: 324, all unaffected** (§3.3).

**External implementers** are the only genuinely unquantifiable population. The mitigation is that
the population is bounded by "people who implemented a result interface against an alpha
prerelease", the fix for them is three lines, and it costs them nothing to do it now versus being
told in 2.0 that it was possible all along.

### 4.3 The asymmetry, stated honestly

Deferring is not neutral. Because a member's type cannot change in a minor version, "defer to
post-1.0" *is a decision to ship option (b)* — a lossy `Errors` plus a bolt-on structured collection
— for the entire 1.x line, or to hold structured errors hostage to a 2.0 that has no other reason to
exist. Option (d)'s own fallback wording concedes the point: a wire `errors` array without a model
change is, per the proposal's §2.1, cosmetic.

**Ruling: in scope, and it should be sequenced early** — the wire shape, the conformance fixtures,
the client round-trip fix and the gRPC `FieldViolation.Field` fill-in all sit downstream of it.

## 5. Consequences

### 5.1 Validation integrations — there are **three**, not two

The proposal names FluentValidation and DataAnnotations. There is a third:

| Integration | Field | Code | Notes |
|---|---|---|---|
| `Benzene.FluentValidation` — `ValidationMiddleware` and `ValidationClientMiddleware` | `ValidationFailure.PropertyName` | `ValidationFailure.ErrorCode` | Both middlewares must change; today both do `.Select(x => x.ErrorMessage)` |
| `Benzene.DataAnnotations` — `ValidationMiddleware` | `ValidationResult.MemberNames` (one `BenzeneError` per member; `null` when empty) | **`null` — unavailable** | `ValidationResult` does not expose the attribute that produced it |
| `Benzene.JsonSchema` — `JsonSchemaValidationErrors.Format` | `x.InstanceLocation` (JSON Pointer, e.g. `/name`) — **currently string-prefixed into the message** | the keyword name (`maxLength`, `required`), currently discarded | A third loss point the proposal missed; it is also the *only* one whose field name is already in wire form |

Three rulings on this:

1. **Emit `PropertyName` and `ErrorCode` verbatim.** FluentValidation's `ErrorCode` defaults to the
   validator type name — `NotEmptyValidator`, not `NotEmpty`. **The proposal's §4 example is
   therefore aspirational and must be corrected.** Do not strip a `Validator` suffix: it would
   corrupt codes an author set deliberately via `.WithErrorCode(...)`, and inventing a normalisation
   is worse than reporting the truth.
2. **Do not transform field names into wire casing.** The validator knows the .NET property path
   (`Name`, `Address.Line1`, `Items[0].Sku`); the *wire* name depends on the serializer in DI, which
   the validation middleware cannot see and which may not be JSON at all. Emit the property path and
   **document** that it is the .NET path. `Benzene.JsonSchema` is the exception and should emit its
   JSON Pointer as-is, because there it genuinely is the wire name. Map empty/`""` to `null` so the
   member is omitted rather than emitted blank.
3. **Accept that `code` is inconsistently populated, and document it.** The proposal argues
   inconsistent population is "worse than absent". Disagree: `code` is optional by design, and
   withholding a real code from FluentValidation users because DataAnnotations cannot supply one
   serves nobody. The obligation is honesty — the capability matrix and each package's `CLAUDE.md`
   must state which integration populates which member.

`Benzene.JsonSchema` should stop prefixing the pointer into the message string once `Field` exists —
otherwise the field appears twice, once structured and once inline.

### 5.2 The client — the round-trip fix comes with this

`src/Benzene.Clients/Common/ClientResultExtensions.cs` currently does
`BenzeneResult.Set<T>(status, errorPayload.Detail)`: a two-error failure round-trips as **one**
error whose text is the joined string. With a structured `errors` array on the wire it becomes:
prefer `errorPayload.Errors` when present, fall back to a single message-only error from `Detail`.
That closes the proposal's §2.3 defect (spec and implementation disagreeing on a normative rule)
rather than merely rewording it.

### 5.3 gRPC gets a free correctness win

`AddRichErrorDetails` can finally populate `google.rpc.BadRequest.Types.FieldViolation.Field` from
`BenzeneError.Field`, and should carry `Code` in the description or a sibling detail. This is
existing, shipped code that has been emitting half a structured error since it was written.

### 5.4 `classification` and `code` — **two concepts, and they stay two**

Reconcile the *documentation* now; do **not** merge the models.

| | `classification` (mesh issue feed, `docs/specification/mesh.md` §4.1) | `code` (`BenzeneError`) |
|---|---|---|
| Cardinality | one per invocation | one per error |
| Vocabulary | **closed** — `exception`, `validation`, `config-wiring`, `dependency`, `contract-drift`, `unclassified` | **open** — owned by the application's validators |
| Derived from | Benzene status + captured exception type, by normative precedence | the validation rule that failed |
| Audience | operator, via the mesh UI | the calling client, branching in code |
| Owner | the framework | the application |

Merging them fails in both directions: forcing application rule codes into a closed vocabulary is
impossible, and opening the mesh vocabulary destroys the fingerprint stability that
`mesh:issues` merge semantics depend on.

What *must* happen now, because it is cheap and prevents a real future mistake: **state normatively
that `code` MUST NOT participate in the mesh issue `fingerprint`.** The fingerprint is
`service|topic|version|classification|discriminator`; an open, per-error, application-owned code
would explode issue cardinality and defeat the merge. One sentence in each spec, no code.

## 6. Risk analysis

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | External alpha consumers who implement `IBenzeneResult` or read `.Errors` fail to compile | Low | Pre-1.0, alpha-only publishing; `CHANGELOG.md` entry required. **Note:** `docs/migration-alpha-to-1.0.md`, which `work/1.0-readiness-checklist.md` claims exists, **does not exist** — it must be created, and this change is its first real entry |
| R2 | `BenzeneError` fails to round-trip through one of the five shipped serializers | Medium | Parameterless ctor + init-only properties (§3.2); require a round-trip test for the STJ default and Newtonsoft before merge |
| R3 | `"field": null, "code": null` clutters every wire error | Low | `[JsonIgnore(Condition = WhenWritingNull)]` on the two optional members — no new dependency (STJ is in-box on `net10.0`); **do not** change the default serializer's global `DefaultIgnoreCondition`, which would silently alter every user payload |
| R4 | `code` present for FluentValidation, absent for DataAnnotations | Low | Accepted and documented (§5.1.3) |
| R5 | Extra allocation per error on the failure path | Low | Failure paths only; success results share a single empty instance. Verify with the existing BenchmarkDotNet suite if the pipeline benchmark moves |
| R6 | Scope creep — this ruling gets read as approving the whole wire shape | Medium | Explicitly does not. This settles the **model**. `errors` on the wire, the RFC-7807 withdrawal and the `type`/`title`/`instance` removal remain the wire/spec ruling's call — now unblocked, not pre-decided |
| R7 | Approving one post-freeze-proposal breaking change invites others | Medium | The bar is stated in §4.1 and is not lowered here. **This is the last change to the result model before the tag**; anything further on `IBenzeneResult` is a 2.0 item |

Also worth recording, found while measuring: existing conformance fixtures survive untouched.
`docs/specification/conformance/README.md` specifies **subset** comparison of `expected.body`, so
adding an `errors` member to the payload does not invalidate the pinned cases in
`envelope-cases.json`. And there is **no Go port in the repository yet** (zero `.go` files), so the
cross-language cost of this change is prospective, not incurred.

## 7. Verdict

**APPROVE — option (a′): structured primary, keeping the name `Errors`; `BenzeneError` in
`Benzene.Abstractions`; land before the 1.0 tag.**

With three binding amendments to the proposal as written:

1. The structured list takes the name `Errors`; the string form becomes an `ErrorMessages()`
   extension in `Benzene.Results`, **not** a second interface member (§3.2, §3.4).
2. Structured factory overloads are limited for 1.0 to `Set`, `ValidationError` and `BadRequest`,
   non-`params` (§3.3). The rest are additive and can follow in 1.x.
3. `Benzene.JsonSchema` is added to the list of validation integrations that must change, and the
   proposal's `"code": "NotEmpty"` example is corrected to FluentValidation's actual
   `NotEmptyValidator` (§5.1).

## 8. Next steps

**Gating, in order** (each behind a green `Benzene.sln` build + full suite; one logical change per
commit):

1. `BenzeneError` + `IBenzeneResult.Errors` type change + `BenzeneResult` factory overloads +
   `ErrorMessages()` extension. Fix the 8 `src/` and 23 `test/` sites in the same commit — the build
   is red until they are.
2. `ErrorPayload`: carry `errors` alongside the unchanged `detail`. Serializer round-trip tests (R2).
3. Validation integrations: FluentValidation ×2, DataAnnotations, JsonSchema (§5.1). **Route to the
   validation product owner** — the field/code semantics per integration are their call to confirm.
4. `ClientResultExtensions` round-trip fix (§5.2); `GrpcMethodHandler` `FieldViolation.Field` (§5.3).
5. Spec + fixtures — **not this ruling's scope**; now unblocked. Includes the one-line
   `code`-vs-`classification` separation and the fingerprint prohibition (§5.4).

**Documentation owed** (mine, and blocking the tag):

- XML docs on `BenzeneError` and the changed `Errors` member — `Benzene.Abstractions` is at 100%
  documented and must stay there.
- `src/Benzene.Results/CLAUDE.md` and `src/Benzene.Abstractions/CLAUDE.md` key-type lists.
- `CHANGELOG.md` under `### Changed` (breaking), and **create `docs/migration-alpha-to-1.0.md`** —
  it is referenced by the readiness docs but does not exist (R1).
- A per-integration table of which validation integration populates `field` and `code`, in the
  capability matrix and in each package's `CLAUDE.md`.

**Version strategy:** no version implications — this is pre-tag, `version.txt` stays `0.0.2` until
the release bump. Post-1.0 the same change would be 2.0-only, which is the entire argument for doing
it now.
