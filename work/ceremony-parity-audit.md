# Ceremony parity across the ports — first audit

The one thing no per-repo agent can see: whether the **same capability costs the same amount of
code** in .NET, Go, TypeScript and Python. A port can be internally tidy and still be the odd one
out. This is the standing record of what has been compared and what was found, so a settled answer
is not re-litigated and an unsettled one is not forgotten.

Method: pick a thing a service must do, count what each port makes you write for it, and separate
*a port is different because its language is different* from *a port is different because nobody
looked*. Only the second is a defect.

---

## 1. "Where does my configuration come from?" — **closed, 2026-08-19**

| Port | Before | After |
|---|---|---|
| .NET | `GetConfiguration()` **abstract** — every service wrote `new ConfigurationBuilder().AddEnvironmentVariables().Build()`; 23 of 50 StartUps were byte-identical | virtual, defaulting to exactly that |
| TypeScript | `getConfiguration?()` optional, but a host fell back to `emptyConfiguration()` — so a startup reached for `process.env` itself (40 such reads) | falls back to `environmentConfiguration()` |
| Go | `App[TConfig].GetConfiguration` optional; `TConfig` is **application-defined** — Benzene does not prescribe its shape | unchanged, correctly |
| Python | no configuration phase on the startup at all — a startup function takes what it needs as arguments | unchanged, correctly |

**The finding was not the ceremony, it was the default.** .NET made you write the common answer;
TypeScript let you skip it but then handed you nothing, so you wrote `process.env` instead. Both
ended with a service reading its environment through a route the framework did not own.

In TypeScript that had a second cost: a value read from `process.env` goes around
`BenzeneTestHost.withConfiguration(...)` entirely, so a component test could not override the value
the service actually read. The override seam existed and did not reach what mattered.

Go and Python are different **by design**, and that is the distinction this audit exists to make.
Go's configuration is a generic type the application owns, so there is no universal default to
supply — inventing a `map[string]string` one would fit the language worse than what is there.
Python threads dependencies as arguments and never had the phase. Neither is a gap.

---

## 2. "Run this service" — closed earlier (2026-08-19)

`BenzeneHost.Run<TStartUp>` (.NET), `BenzeneHost.build` (TypeScript), the worker host (Python),
composition-root shorthands (Go). Recorded here for completeness: the same capability was missing
or unsafe in all four ports at once, which is the pattern this audit is meant to catch early.

---

## Still open

- **`errors` on a result.** .NET and TypeScript now carry structured `BenzeneError[]`
  (`message`/`field`/`code`). Go carries `[]string` on the result and structures them only at the
  wire edge; Python carries `tuple[str, ...]`. So a Go or Python handler still cannot attach a
  `field` or `code` to a validation failure, and its problem document is message-only. Whether that
  is worth changing depends on whether those ports intend to serve schema validators that produce
  field paths — a question for those ports' owners, not a defect to fix unilaterally.
*(the descriptor-hash item that stood here is closed - see section 3.)*

---

## 3. "Does the descriptor hash cover the same things everywhere?" — **closed, 2026-08-20**

It did not, and worse, two ports could not have told you either way.

`mesh-descriptor-cases.json` pins four hash properties: invariant to `instanceId`, sensitive to
`serviceVersion`, to the topic set, and to the produced-topic set. Every port reads those flags out
of the fixture and asserts the ones the fixture asks for. Three of the four had been renamed by the
role inversion (spec f45a187) from `sensitiveToConsumes` to `sensitiveToProduces`.

| Port | Key it read | Effect |
|---|---|---|
| .NET | `sensitiveToProduces` | asserted |
| Python | `sensitiveToProduces` | asserted |
| Go | `sensitiveToConsumes` | **silently skipped** |
| TypeScript | `sensitiveToConsumes` | **silently skipped** |

**The finding was not the stale name, it was the shape of the guard.** Every port wrote the same
idiom — read the flag, and if it is falsey, quietly do nothing:

```go
if !fixture.Hash.SensitiveToConsumes { t.Skip("not asserted by the fixture") }
```

```ts
if (!fixture.hash.sensitiveToConsumes) return;
```

A key the fixture sets to `false` and a key the fixture has never heard of decode to the same value,
and they mean opposite things. So the rename did not break the two runners — it disabled them, and
both suites went on passing while asserting nothing at all about produced topics. Neither
implementation was wrong; both were simply unverified, which a green CI is supposed to rule out.

All four ports now distinguish absent from false (`*bool` in Go, optional in TypeScript, `bool?` in
.NET, `key not in spec` in Python) and fail loudly on absent. A fixture that deliberately turns a
property off still skips. Each was verified by renaming the key in the vendored fixture and
confirming the suite goes red, then restoring it.

The transferable rule, and the reason this belongs in a *parity* audit: **a conformance runner must
never treat "the fixture didn't ask" as indistinguishable from "the fixture asked for nothing".**
Ports vendor snapshots of a canonical fixture that other people rename; the drift is routine, and
only the runner is positioned to notice it.

This also exposed a second gap in Go, recorded here because it is the same failure mode one level
up: CI's `go test` named a hand-written list of workspace modules that had fallen nine modules
behind `go.work`, and a descriptor test inside one of them
(`examples/azure-functions-mesh`) had been asserting the pre-inversion provider/consumer direction,
failing on every run, seen by nobody. `scripts/modules.sh` now derives that list from `go.work`, and
CI and the READMEs share it. Coverage that is enumerated by hand goes stale the same way a fixture
key does.

---

## 4. "Which fixtures does each port actually run?" — **audited, 2026-08-20**

Vendoring a fixture is not running it. Every port carries a snapshot of the canonical set; nothing
checked that a snapshot was *read*.

| Fixture | .NET | Go | TypeScript | Python |
|---|---|---|---|---|
| `problem-details-cases.json` | ✅ | ❌ → ✅ | ✅ | ❌ → ✅ |
| `mesh-version-order-cases.json` | ✅ | ❌ | ❌ | ❌ |
| `mesh-service-version-cases.json` | ❌ | ❌ | ❌ | ❌ |
| everything else | ✅ | ✅ | ✅ | ✅ |

`problem-details-cases.json` is not optional: conformance/README.md's claims table makes its
`registry` and `envelopeCases` groups **required for the Benzene Core claim**, and `httpRules`
required for every HTTP binding a port ships. Go and Python claimed Core, shipped an HTTP binding,
vendored the fixture, and ran none of the three. Both now run all three (§5 below is what it took).

The other two rows are legitimately conditional — `mesh-version-order-cases` is required only of a
port that orders service versions, and `mesh-service-version-cases` only of a collector claiming
service-version identity. But all four ports vendor them, which reads like intent rather than a
decision, and **no port runs the service-version fixture at all**. Left open deliberately: it is a
claim decision for each port's owner, not a defect to fix unilaterally. Recorded so it is a decision
rather than an oversight.

Go and Python also had **no conformance drift check** — the job .NET and TypeScript run to diff the
vendored snapshot against canonical. Both now have it, and it runs in both directions: a drifted
fixture is the obvious half, but a canonical fixture *missing* from the snapshot is the half that
bites, since a runner cannot notice a fixture it was never given.

---

## 5. "How much of an error can a handler express?" — **closed, 2026-08-20**

The item §3 left open ("whether that is worth changing depends on whether those ports intend to
serve schema validators that produce field paths — a question for those ports' owners") was not
actually open. The spec answers it: the canonical `conformance:problem` handler MUST return one
structured error carrying `message`/`field`/`code`, and those envelope cases are required for Core.

| Port | Before | After |
|---|---|---|
| .NET | `BenzeneError[]` on the result | unchanged |
| TypeScript | `BenzeneError[]` on the result | unchanged |
| Go | `Errors []string` — nowhere to put a field or a code | `[]benzene.Error`, an **alias** of `wire.ProblemError` |
| Python | `errors: tuple[str, ...]` | `tuple[BenzeneError, ...]`, every factory takes either |

The two ports took the shape their language allows, which is the distinction this audit exists to
make. Python can accept a union, so one `ErrorLike` parameter type covers strings and structured
errors on *every* existing factory and nothing new is needed. Go has no overloads, so it gains
exactly two functions — `FailWith` for any status and `ValidationErrorWith` for the one where a
field and a code are nearly always known. Neither port made the plain-string path any longer.

Both aliased rather than copied (`benzene.Error = wire.ProblemError`), so the value a handler builds
*is* the value that reaches the wire — no second shape to keep in step, no conversion to drop a
member. And both kept the type-erased/message-only view intact (`ResultErrors() []string`,
`Result.messages`), so every binding that only ever wanted prose was untouched by the change.

The payoff is concrete, not theoretical: `benzene-pydantic` was gluing pydantic's own `loc` and
`type` into a `"field: message"` string and throwing the structure away. They now cross into `field`
and `code` unchanged — the same rule .NET's FluentValidation adapter already followed.

---

## 6. "Does the envelope state whether it succeeded?" — **closed, 2026-08-20**

Found while wiring §4's fixture, and the sharpest example in this document of why a weak runner is
worse than no runner.

`isSuccessful` is **required** on every response envelope (wire-contracts §1.2) and is the
authoritative signal — a receiver MUST prefer it over anything derived from `statusCode` text, which
is the only signal an application-defined status has. All 17 envelope cases assert it.

**Python emitted it nowhere and checked it nowhere.** Both of its envelope-case loops — the runner
and the parametrized pytest test beside it — skipped `isSuccessful` and `bodyExclude`, so 17
success-signal assertions and 11 withdrawn-member assertions passed without being checked, over a
port that genuinely did not implement the member. .NET and TypeScript check both; Go's runner was
strengthened to earlier in the same sweep.

Two loops is how that happens. Python now has one checker that both call, as Go now does.

The same investigation turned up a functional bug the missing member was hiding: an unhealthy health
check answered `service-unavailable`, the encoder classified it a failure by status class, and
**replaced the per-check report with a problem document** — discarding exactly the information
somebody hits a health endpoint for. §1.3 carves this case out, and .NET has had the escape hatch
all along (`BenzeneResult.Set(ServiceUnavailable, message, true)`), Go as `SetResult`, TypeScript in
`create`. Python had no way to state a success classification at all. `Result.set` closes it, and
`benzene.http` now prefers the envelope's `isSuccessful` over the status class so the carve-out
survives the HTTP hop too.

**The rule this adds:** a required wire member that no runner checks is a member the port may simply
not have. Check the *envelope*, not just the body.

---

## 7. "Can a validator say which field failed?" — **closed, 2026-08-20**

§5 gave every port a result that *can* carry a field and a code. This is the other half: whether the
things that actually produce validation errors bother to fill them in. Mostly they did not, and the
information was there all along.

| Port | Adapter | Before | After |
|---|---|---|---|
| .NET | FluentValidation, DataAnnotations, JsonSchema | `BenzeneError` with Field/Code | unchanged |
| TypeScript | ajv | JSON Pointer **glued onto the message text** | `field` + `code` |
| TypeScript | zod, joi, yup | message only; path and rule discarded | `field` + `code` |
| Python | pydantic | `"field: message"` glued into one string | `field` + `code` |
| Go | `validation.Validator[T]` | `[]string` — nowhere to put either | `[]benzene.Error` |

Every one of these libraries hands its adapter the message, the location, and the rule identifier.
Six of the eight adapters kept the message and dropped the rest — pydantic's `loc`/`type`, ajv's
`instancePath`/`keyword`, zod's `path`/`code`, joi's `path`/`type`, yup's `path`/`type`. TypeScript's
own `JsonSchemaValidationErrors` is documented as a port of .NET's `Format`, and had ported the
shape that predated structured errors.

`"/name: must NOT have more than 5 characters"` is a string to print. `{field: "/name", code:
"maxLength"}` is an error a UI can attach to an input and a client can branch on. Nothing here
needed new plumbing — `BenzeneError` and the union-accepting factories already existed in every
port. The adapters simply had not been brought across.

**The rule, settled here so the next adapter follows it:** `field` is the validator's own path,
verbatim — the same rule .NET already followed, where FluentValidation arrives dotted and JsonSchema
arrives as a pointer. Where a validator reports a path as an array with no native string form (zod,
joi), render a JSON Pointer, RFC 6901-escaped; that is wire-contracts §1.3's tie-breaker for a
schema-based validator with no opinion of its own.

Go took the shape its language allows again: no overloads, so instead of a second interface and a
type switch, `Messages()` adapts a plain `[]string` validator in one call and the interface itself
is structured.

---

## Still open (after 2026-08-20)

- **A client decoding a peer's problem document.** Go's `httpclient` and `awslambdaclient` now
  rebuild a failed `Result` from the peer's problem document with the structured errors intact
  (`ErrorPayload.Problems()`); before tonight they flattened it to messages, and the round trip is
  now pinned end to end. .NET's `HttpStatusCode.Convert` and TypeScript's `HttpContextConverter` map
  the HTTP **status code** to a result and read no errors from the body at all — a failed call
  through them yields a result with an empty `errors`.

  The two families are not the same shape, which is why this is a question and not a defect: Go's
  clients speak the wire **envelope**, so the body always *is* a problem document, while .NET's and
  TypeScript's HTTP client calls an arbitrary verb+path endpoint whose body is whatever that endpoint
  returns. Reading `errors` there means guessing that an arbitrary peer speaks RFC 9457. No
  conformance fixture requires it, and .NET and TypeScript agree with each other today, so changing
  it is a deliberate cross-port decision for the owners rather than something to fix in passing. The
  question worth answering: should a failure from a *Benzene* peer over the HTTP client carry that
  peer's `errors`, and if so, how does the client know the peer is one?

- **`mesh-service-version-cases.json` is vendored by all four ports and run by none.** A claim
  decision per port (§4), not a defect. Recorded so it stays a decision.
