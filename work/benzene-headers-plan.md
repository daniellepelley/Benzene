# `benzene-headers` — implementation plan (deferred, execute after the repo split)

**Status:** PLAN — **deliberately not executed.** Documented now, implemented later.
**Last Updated:** 2026-07-25
**Purpose:** The executable plan for `work/benzene-headers-design.md`. Written to be run **after**
the repo split (`work/repo-split-plan.md`) has landed, because this change touches both sides of
that split and doing it now would create a near-unmergeable conflict with the in-flight file moves.

> **Why this is a document and not a commit.** A large refactor is in flight moving the .NET port
> into `benzene-dotnet`. Code changes made now would collide with those moves and be painful to
> reconcile; a single new document will not. So the design is captured in full detail — enough to
> execute mechanically later, by someone who did not write it.

---

## 1. This is the first spec change to cross the new repo boundary

Per `work/repo-split-manifest.md`, this work lands in **both** repos:

| Piece | Repo | Notes |
|---|---|---|
| `docs/specification/wire-contracts.md` §2, `transport-bindings.md` | **`benzene`** | The spec **stays**. Canonical. |
| `docs/specification/conformance/*.json` | **`benzene`** | Canonical fixtures. |
| Every `src/`/`test/` change below | **`benzene-dotnet`** | The .NET port **moves**. |
| `test/conformance-fixtures/` snapshot + `SPEC_VERSION` | **`benzene-dotnet`** | Vendored copy; the CI drift-check compares it to `benzene`. |

**So the ordering is fixed, and it is not optional:**

1. **Spec first, in `benzene`.** The wire contract is the source of truth; changing the port first
   would make the port the de-facto spec.
2. **Then the .NET port, in `benzene-dotnet`.**
3. **Then re-vendor the fixture snapshot** and confirm the drift-check passes.

Between (1) and (3) the drift-check is *expected* to fail — that is the machinery working, not a
break. Worth saying out loud in the PR so nobody "fixes" it by editing the snapshot alone.

This is also a useful first exercise of the split's own contract: a spec change, a port catching up,
and the drift-check proving they reconciled.

## 2. Phase A — the rename (go-live critical)

`_benzeneHeaders` → `benzene-headers`. Small, contained, and **free only until the 1.0 tag**; after
that it is a major-version migration. Clean break, no dual-accept — consistent with the topic-id
ruling (no installed base: `version.txt` is `0.0.2`, no tags).

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
| 1 | Phase A spec rename | `benzene` | — |
| 2 | Phase A code rename | `benzene-dotnet` | Before the 1.0 tag |
| 3 | Re-vendor fixtures; drift-check green | `benzene-dotnet` | Closes Phase A |
| 4 | Phase B1–B4 | `benzene-dotnet` | Either side of the tag |
| 5 | Phase B5 spec | `benzene` | With or before B4 |

**Phase A is on the 1.0 critical path** (`work/1.0-release-plan.md`, Tier 1.0-SPEC) because it is a
wire contract. Phase B is not — it is additive and can follow the tag safely.
