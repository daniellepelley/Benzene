# `benzene-headers` — implementation plan

**Status: READY TO EXECUTE — blocked only on a maintainer go/no-go.** The deferral reason has
expired (see below); nothing else stands in the way, and Phase A is on the 1.0 critical path.
**Last Updated:** 2026-07-25 (plan) · 2026-08-20 (re-statused, and the blast radius corrected)
**Purpose:** The executable plan for `work/benzene-headers-design.md`.

> **Why this was deferred, and why that no longer applies.** The plan was written to run *after* the
> repo split (`work/repo-split-plan.md`), because code changes made while the .NET port was being
> moved into `benzene-dotnet` would have collided with the in-flight file moves. **The split is
> complete**: all four ports live in their own repositories with conformance drift-checks running
> (`work/repo-split/STATUS.md`, all phases done). The blocker is gone. What replaces it is not
> another blocker but a decision — Phase A is a **breaking wire change, free only until the 1.0
> tag**, so it needs a maintainer go/no-go, and then a sequenced run.

> **The blast radius grew while this sat.** The plan below was written when there were two
> repositories. There are now five: `_benzeneHeaders` appears in **benzene-dotnet, benzene-go,
> benzene-typescript and benzene-python**, not just .NET. §1 and §5 are corrected for that; the
> Phase A body (§2) still lists only the .NET symbols, and the equivalent sweep in each of the other
> three ports has to be worked out at execution time rather than read off this page.

---

## 1. Ordering — spec first, then every port, then re-vendor

Per `work/repo-split-manifest.md`, this work lands in **both** the spec repo and every port:

| Piece | Repo | Notes |
|---|---|---|
| `docs/specification/wire-contracts.md` §2, `transport-bindings.md` | **`benzene`** | The spec **stays**. Canonical. |
| `docs/specification/conformance/README.md` | **`benzene`** | Carries the note explaining why the key is *not* pinned by a fixture — that note has to change with the key. |
| Every `src/`/`test/` change below | **`benzene-dotnet`** | The .NET port. |
| The equivalent change | **`benzene-go`, `benzene-typescript`, `benzene-python`** | Each ships the same embedded-headers key; each needs its own sweep. |
| `test/conformance-fixtures/` snapshot + `SPEC_VERSION` (and each port's equivalent) | **each port** | Vendored copy; the CI drift-check compares it to `benzene`. |

**The ordering is fixed, and it is not optional:**

1. **Spec first, in `benzene`.** The wire contract is the source of truth; changing a port first
   would make that port the de-facto spec.
2. **Then every port**, .NET first as the reference runner, then Go, TypeScript and Python. A port
   that has renamed cannot exchange an EventBridge message with one that has not, so the window
   between the first and last port is a real cross-language interop outage — keep it short, and do
   not start until all four can be finished.
3. **Then re-vendor** each port's fixture snapshot and `SPEC_VERSION`, and confirm the drift-check
   is green.

Between (1) and (3) the drift-check is *expected* to be red — that is the machinery working, not a
break. Say so in each PR, so nobody "fixes" it by editing a snapshot alone.

**One correction to that expectation, checked 2026-08-20.** `_benzeneHeaders` is **deliberately not
pinned by any fixture** — `docs/specification/conformance/README.md` says so explicitly, precisely
because it was scheduled to be renamed. And the ports' drift-checks diff `*.json` only, not the
conformance `README.md`. So on the current shape of the change, **the drift-check will stay green
throughout and prove nothing**: it is not the safety net for this rename. The real gate is each
port's own EventBridge tests plus a repo-wide grep for the old spelling, per §2's verification step.
If a fixture pinning the key is added as part of the rename — worth considering, since the reason
for not pinning it disappears the moment it is renamed — then the red-in-between expectation above
becomes true again, and that is a better outcome than a silent one.

## 2. Phase A — the rename (go-live critical)

`_benzeneHeaders` → `benzene-headers`. Small, contained, and **free only until the 1.0 tag**; after
that it is a major-version migration. Clean break, no dual-accept — consistent with the topic-id
ruling (no installed base: `version.txt` is `0.0.3` as of 2026-08-20, still no tags — the window is
open but it is the only thing holding it open).

**Spec (`benzene`):**
- `wire-contracts.md` §2 — the `_benzeneHeaders` row becomes `benzene-headers`. Keep the tier (**D**,
  transport binding) and the existing note about *why* it is payload-embedded on EventBridge.
  **Replace** the "its form differs deliberately … camelCase JSON convention" sentence: the form no
  longer differs, and the new sentence should say the opposite — it is a **header name**, so it uses
  the same lowercase kebab-case as every other header even where the carrier is a payload field,
  because it names a header rather than being one of the payload's own fields.
- `transport-bindings.md` — the EventBridge binding section, same rename.

**Code (`benzene-dotnet`), by type rather than path** (paths change in the move):
- `EventBridgeMessageHeadersGetter.EmbeddedHeadersKey` — the constant. Value → `"benzene-headers"`.
- `EventBridgeContextConverter<T>.EmbeddedHeadersKey` — the outbound twin.
- `OutboundEventBridgeContextConverter.EmbeddedHeadersKey` — already an alias of the above; verify it
  still aliases rather than re-declaring after the move.
- `EventBridgeMessageBodyGetter` — references the key in its doc comment and skip-logic.
- Doc comments in `EventBridgeBenzeneMessageClient` and the two converters.
- Any EventBridge test fixture with a literal `_benzeneHeaders`.

**Verification:** build; the EventBridge tests; a repo-wide grep for `_benzeneHeaders` returning
nothing (**including** the vendored fixture snapshot).

## 3. Phase B — packed headers (additive, either side of the tag)

Nothing here changes existing behaviour: every default stays as it is, and this only adds an opt-in
capability plus a fallback that fires where lookup previously failed.

### B1. A shared packed-headers codec
One implementation, used by every transport, so the format cannot drift per binding:
- `Pack(IDictionary<string,string>) → string` — a flat JSON object, string→string.
- `Unpack(string) → IDictionary<string,string>` — tolerant: malformed JSON yields empty, never
  throws (a bad header must not fail the invocation).
- **Flatten once:** a `benzene-headers` key *inside* the bag is ignored, never recursed.
- Home: the lowest package every transport already references (`Benzene.Abstractions.Messages`
  alongside `MessageVersionHeaders`, or `Benzene.Core.Messages` — pick at implementation time by
  what the transports actually reference **after** the move; do not add a project reference for it).

### B2. Inbound — a composite topic getter
A `CompositeMessageTopicGetter<TContext>` taking an ordered `IMessageTopicGetter<TContext>[]`,
returning the first resolved topic. (Precedent for composition in this codebase:
`CompositeMessageHandlersFinder`.) Default order per binding:

1. **Native carrier**, where one exists — EventBridge `detail-type`, Kafka's own topic.
2. **The `benzene-topic` header/attribute** — the existing getter, unchanged.
3. **The packed bag** — a new per-transport packed getter: read `benzene-headers`, unpack, take
   `benzene-topic`.

Registration becomes the composite by default; the single-purpose getters stay registerable on their
own for a deployment that wants exactly one behaviour. **All 25 existing `IMessageTopicGetter`
implementations keep working untouched** — they become members of a chain rather than being replaced.

### B3. Inbound — the header bag merges by the same rule
Packed bag as the base layer, individual headers overlaid on top; **an individual header wins on
conflict**. Same precedence as the topic, so topic and headers can never disagree about which source
is authoritative.

### B4. Outbound — the opt-in switch
- Default unchanged: individual headers, `benzene-topic` among them.
- `packHeaders: true` on the client/converter: take the accumulated header dictionary, **add the
  topic into it**, `Pack`, write the single `benzene-headers` attribute.
- **Pack at the terminal converter, never earlier.** This is the invariant that keeps "headers are
  additive with middleware" true in both modes — middleware keeps adding to the dictionary and never
  needs to know which mode is configured. Any implementation that packs mid-pipeline breaks it.
- One switch per client. Not per-header — that would produce wire shapes nobody can predict.

### B5. Spec
`wire-contracts.md` §2: `benzene-headers` graduates from a D (EventBridge binding detail) to a
**C (optional add-on) available on any transport**, with EventBridge noted as the case where it is
mandatory because the transport has no metadata channel. Document the precedence rule (§B3) and the
motivation (SQS's 10-attribute cap; a service with topic + version + correlation + trace context is
at five before the application adds anything).

## 4. Decisions already taken (do not reopen at implementation time)

From `work/benzene-headers-design.md` §3 — recorded here so the implementer is not re-litigating:

1. Encoding: **flat JSON object, string→string.** No nesting.
2. In packed mode the topic **is always in the bag** — self-contained beats a per-binding table.
   Bindings with a native carrier prefer it on read (§B2 order).
3. Nested `benzene-headers` inside the bag: **flatten once, ignore, never recurse.**
4. Packing trades attribute *count* for attribute *size*: **document, do not enforce a limit.**
5. **One switch per client**, not per header.

## 5. Sequencing summary

| Order | What | Repo | Gate |
|---|---|---|---|
| 0 | **Maintainer go/no-go on Phase A** | — | Everything below waits on this |
| 1 | Phase A spec rename (incl. the conformance `README.md` note) | `benzene` | — |
| 2 | Phase A code rename | `benzene-dotnet` | Before the 1.0 tag |
| 3 | Phase A code rename | `benzene-go`, `benzene-typescript`, `benzene-python` | Same window as 2 — a renamed port cannot talk EventBridge to an un-renamed one |
| 4 | Re-vendor fixtures + `SPEC_VERSION`; drift-check green | every port | Closes Phase A |
| 5 | Phase B1–B4 | `benzene-dotnet`, then the other ports | Either side of the tag |
| 6 | Phase B5 spec | `benzene` | With or before B4 |

**Phase A is on the 1.0 critical path** (`work/1.0-release-plan.md`, Tier 1.0-SPEC) because it is a
wire contract. Phase B is not — it is additive and can follow the tag safely.
