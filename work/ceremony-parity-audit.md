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
