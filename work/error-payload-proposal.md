# Error Payload — investigation and proposal

**Status:** PROPOSAL for maintainer ruling — investigation complete, recommendation made, nothing
applied. Task #28 (`work/spec-review-2026-07-25.md` §2).
**Last Updated:** 2026-07-25
**Purpose:** Answer "is there a better error payload, and is there a standard Benzene should
follow?" — grounded in what the code actually does today, not what the spec says it does.

---

## 1. What we have today (grounded)

**Spec** (`docs/specification/wire-contracts.md` §1.3): on failure the response `body` is

```json
{ "status": "not-found", "detail": "No handler found for topic order:create" }
```

with `type`, `title`, `instance` *"Reserved (RFC 7807 alignment)"*, and the rule *"Clients recover
`errors` from `detail`"*.

**Code:**

| Where | What it does |
|---|---|
| `src/Benzene.Results/ProblemDetails.cs` | `Type`, `Status` (**string**), `Title`, `Detail`, `Instance` |
| `src/Benzene.Results/ErrorPayload.cs` | `ErrorPayload(status, string[] errors)` → `Detail = string.Join(", ", errors)` |
| `src/Benzene.Results/BenzeneResult.cs` | `public string[] Errors { get; }` — the result model itself carries only strings |
| `src/Benzene.FluentValidation/ValidationMiddleware.cs` | `validationResult.Errors.Select(x => x.ErrorMessage).ToArray()` |
| `src/Benzene.Clients/Common/ClientResultExtensions.cs` | deserializes `ErrorPayload`, then `BenzeneResult.Set<T>(status, errorPayload.Detail)` |
| `docs/specification/conformance/envelope-cases.json` | pins `{"status": "bad-request", "detail": "first error, second error"}` |

## 2. Findings

### 2.1 There are **two** information-loss points, and the first one is upstream of the wire

FluentValidation's `ValidationFailure` carries `PropertyName`, `ErrorMessage`, `ErrorCode` and
`AttemptedValue`. `ValidationMiddleware` keeps **only `ErrorMessage`**. So the field name is
destroyed *before serialization is even reached* — and `BenzeneResult.Errors` is `string[]`, so the
result model could not carry it anyway.

**Consequence: this is not a serialization fix.** Changing only the wire shape would add an
`errors` field that is always empty of structure. The model and the validation integrations have to
change first, or the change is cosmetic.

### 2.2 `status` actively conflicts with the standard the spec claims alignment with

RFC 7807 / RFC 9457 define `status` as **the integer HTTP status code**. Benzene's `status` is a
**string** carrying the Benzene status (`not-found`). Same member name, different type and meaning,
in a payload the spec advertises as *"problem-details-shaped"* with *"RFC 7807 alignment"*.

A generic problem-details consumer — and they are everywhere (ASP.NET, Spring, Quarkus, many API
gateways) — will either fail to deserialize or silently mis-read it. **This is worse than the
payload being "basic": it is mislabeled.** Meanwhile `type`/`title`/`instance` exist solely to
support that claim and are, in practice, always null.

### 2.3 The spec's round-trip rule doesn't work, and the implementation doesn't follow it

The spec says clients recover `errors` from `detail`. Splitting on `", "` is unsafe by
construction — error messages contain commas routinely ("Name must not be empty, and must be under
50 characters"). And the .NET client doesn't attempt it: it passes the **whole joined string** as a
single error. So a two-error failure round-trips as one error, and spec and implementation
disagree about a normative rule.

### 2.4 What Benzene knows and throws away

The pipeline holds, at the moment of failure: the failing field, the validation rule that failed
(`ErrorCode`/`ValidationConstants`), the Benzene status, and — since the drains-up work — the
converted exception type and a resolution hint (`MessageErrorState`, and the mesh issue feed's
closed classification vocabulary in `docs/specification/mesh.md` §4.1, which already includes
`validation`). The error payload is the one place a *caller* could see any of that, and it sees a
joined string. That gap is the actual product cost: a client cannot render a form-field error, and
a caller cannot branch on a machine-readable code.

## 3. The options

| | Option | Verdict |
|---|---|---|
| A | Leave as-is | Rejected — 2.2 (mislabeled) and 2.3 (broken normative rule) are defects regardless of the richer-errors question |
| B | **Full RFC 9457 compliance**: `status` becomes the integer HTTP code, Benzene status moves to an extension member | **Rejected** — see below |
| C | **Problem-details-*inspired*, explicitly not RFC 9457**, with a structured `errors` array | ✅ **Recommended** |
| D | Per-transport shapes (RFC 9457 over HTTP, Benzene-shaped elsewhere) | Rejected — two shapes to specify, test, and port to every language, to serve consumers who are not Benzene clients |

**Why B is rejected.** RFC 9457 is *Problem Details for **HTTP** APIs*. Benzene is transport-neutral
by its central promise — the same handler answers over SQS, Kafka, EventBridge and gRPC. An
integer HTTP status code in the error payload of an SQS message is a fabricated number: there was
no HTTP response, and the mapping is an invention of the binding layer. Manufacturing an HTTP
artefact on a queue to satisfy a spec that only governs HTTP is exactly the kind of dishonesty this
project refuses elsewhere. (It would also duplicate the envelope's own `status`.)

The honest move is not to align harder — it is to **stop claiming an alignment we don't have**.

## 4. Recommended shape (option C)

```json
{
  "status": "validation-error",
  "detail": "Name must not be empty, Age must be greater than 0",
  "errors": [
    { "message": "Name must not be empty",     "field": "name", "code": "NotEmpty" },
    { "message": "Age must be greater than 0", "field": "age",  "code": "GreaterThan" }
  ]
}
```

| Field | Type | Rules |
|---|---|---|
| `status` | string, required | The Benzene status, mirroring the envelope. Explicitly **not** RFC 9457's `status`. |
| `detail` | string, required | Human-readable summary; the messages joined. **Kept** — it is what every existing reader uses. |
| `errors` | array, optional | Structured errors, **in order**. When present, authoritative. |
| `errors[].message` | string, required | The human-readable message. |
| `errors[].field` | string, optional | The offending field/property. Omitted for errors that aren't field-scoped. |
| `errors[].code` | string, optional | Machine-readable rule/error code (e.g. `NotEmpty`) for callers that branch. |

**Array of objects, not ASP.NET's `errors: { field: [messages] }` dictionary.** The dictionary is
more familiar to .NET developers, and that is a real cost — but it cannot represent an error with
no field (`BenzeneResult.Set(status, "Insufficient funds")` is the common case in handler code),
it cannot carry a per-message code, and it loses ordering. An array is trivially projected *into* a
dictionary client-side; the reverse is lossy. It is also the simpler thing to produce in Go.

**Drop `type`/`title`/`instance`** from the normative table. They exist only to support the RFC
claim being withdrawn. Readers must ignore unknown members anyway, so anyone emitting them stays
compatible.

**Backward compatibility is good:** `status` and `detail` are unchanged, so every existing reader —
including the current .NET client and the existing conformance fixtures — keeps working untouched.
`errors` is purely additive. This can ship without a migration.

## 5. What has to change, in order

1. **The result model** — `BenzeneResult.Errors` is `string[]`. Introduce a structured error type
   (`BenzeneError { Message, Field?, Code? }`). **Open question for the core PO:** make the
   structured list primary with `string[] Errors` kept as a derived convenience projection
   (preserves source compatibility for the many call sites that read `Errors`), or add a parallel
   collection (uglier, two sources of truth — not recommended). This is the deepest and most
   invasive part, and it is only free before the 1.0 tag.
2. **The validation integrations** — `ValidationMiddleware` must carry `PropertyName` →`field` and
   `ErrorCode` → `code` instead of discarding them. Every validation integration
   (`Benzene.FluentValidation`, DataAnnotations) needs the same treatment, or the field is
   inconsistently populated, which is worse than absent.
3. **`ErrorPayload`** — emit `errors` alongside the existing `detail`.
4. **The client** — populate structured errors on the recovered result rather than treating the
   joined `detail` as one error (fixes 2.3).
5. **Spec + fixtures** — rewrite §1.3: withdraw the RFC 7807 claim, state that the shape is
   problem-details-*inspired* and transport-neutral **and say why**, fix the broken "recover
   `errors` from `detail`" rule, document `errors`. Add fixture cases; existing cases stay valid.
6. **Go port** — the shape is deliberately trivial there.

## 6. Constraints honoured

- **Transport-neutral** — no HTTP artefacts in the payload (the reason B is rejected).
- **Cross-language** — three optional string members; no URIs to resolve, no registry to maintain.
- **Secret-safety** — `message` carries validation messages the application already chose to
  surface. The existing rule that exception *messages* never enter error data (`HealthCheckError`,
  and the mesh feed's exception-*type*-only rule) is untouched, and `code` gives callers something
  to branch on **without** widening what leaks.
- **Coherence with shipped work** — `code` is the caller-facing sibling of the mesh issue feed's
  `classification`; the two should be reviewed together so Benzene has one error model rather than
  two half-models.

## 7. Recommendation

Adopt **option C**. Take the `BenzeneResult` model decision (§5.1) first, since everything else
depends on it and it is the only genuinely breaking part — and it is free only until the tag.

If the model change is judged too invasive for 1.0, the fallback that still fixes real defects is:
withdraw the RFC claim, drop `type`/`title`/`instance`, and fix the spec's broken round-trip rule —
leaving structured `errors` to a post-1.0 additive release. **Say so explicitly if choosing this**,
because the structured shape is much cheaper to add before there are clients than after.
