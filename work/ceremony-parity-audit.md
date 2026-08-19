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
- **Descriptor-hash coverage flags.** Named `sensitiveToProduces` everywhere after the role
  inversion; not yet checked that every port asserts the same set.
