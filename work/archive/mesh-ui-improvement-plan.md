> ARCHIVED 2026-08-20: actioned — Waves 1–3 DONE in-file with commits (benzene-ui `998f7fa`, `e54351c`, `d2e2093`); open product-owner decisions extracted to `work/remaining-issues-plan.md`.

# Mesh UI — improvement plan, 2026-08-18

The output of a timeboxed review of the whole mesh UI by three independent reviewers — information
architecture (`mesh-ux-designer`), the visual system (`mesh-visual-designer`), and an adversarial
critic (`mesh-ux-critic`) grading each screen against its owned question — all held to
`work/mesh-ui-aims.md`. Source audited at benzene-ui `5d82aac`; the critic drove the built bundle
in a browser against the sample estate, both themes, two widths.

**The convergent verdict, reached three times independently:** the largest source of mess is not
missing design — it is that **the rulings already made have not been executed**. Discussion (ruled
deleted 2026-08-16), the Compose route (ruled merged), the four estate banners (ruled one block),
the withdrawn runbook copy, the unreachable Issues queue and the unroutable Topics catalogue are
all still shipping. Wave 1 is therefore enforcement, not design, and carries zero design debate.

## The scorecard (critic, 1–5)

| Screen | Owned question | Cognitive load | Hierarchy | Accessibility |
|---|---|---|---|---|
| Estate | 3 | **2** | 3 | 3 |
| Service | **5** | 4 | 4 | 3 |
| Topic | 4 | 3 | 4 | 4 |
| Changes | 4 | **2** | 3 | 4 |
| Issue | 4 | 5 | 4 | 3 |
| Test Console | 4 | 4 | 4 | 4 |

**Worst screen: Estate** — the only one whose owned question ("what state, what first?") is never
answered anywhere on it; the reader synthesises it from five tiles, up to four banners and five
equal-weight cards of which one matters. **Best band in the product:** the Service page's Contract
card — verdict, obligation, and a named counter-party in three lines. **Strongest surface nobody
can find:** Issues, reachable only through a conditional "see all".

Hover-only `title` attributes counted live: 30 Estate, 33 Service, 15 Topic, 11 Changes — 87
occurrences shipped. One measured WCAG failure: the light-theme `breaking` badge at 3.63:1.

---

## Wave 1 — enforce the rulings — **DONE** (benzene-ui `998f7fa`, 2026-08-20)

All seven landed together, plus one defect the new tests caught rather than review: the verdict
sentence scored green on a healthy estate that still had an outstanding breaking move, because
`summary.worst` only knows about health. Recommendations taken as offered: Value → **Retire**, and
the estate's Recent-flows band **deleted**. The compose merge carried capability across rather than
dropping it — the console gained topic-first entry and version-aware seeding, and the compose tests
were repointed at it. 523 UI tests green, 451 .NET.

| # | Move | Files | Size |
|---|---|---|---|
| 1.1 | **Delete Discussion** everywhere: the two cards, `Thread.tsx`, `Composer.tsx`, `annotationsSlice`, `loadAnnotations`, the selectors, the two `MeshApi` members | `ServicePage.tsx:271`, `TopicPage.tsx:327`, sections, store, `App.tsx:59` | M |
| 1.2 | **Merge Compose into Test Console**: delete `ComposePage.tsx`; `parseHash` translates `#compose/<topic>` to the test route (R11 — bookmarks survive); Topic's action targets `page:'test'` with topic+version | `ComposePage.tsx`, `routing.ts`, `TopicPage.tsx:118–125` | S–M |
| 1.3 | **Rewrite the withdrawn runbook copy** (§3.2) and the `raw (benzene-message)` transport label (R7) | `TestConsolePage.tsx:88–92`, `MessageComposer` | XS |
| 1.4 | **Four estate banners → one `DivergenceBlock`**: heading *Declared and observed disagree (N)*, one row per class. Build as a shared section — `ServicePage.tsx:150–168` has two more paragraphs of the same species that adopt it for free | `FleetPage.tsx:140–187`, new section, `ServicePage.tsx` | S |
| 1.5 | **Estate verdict sentence**: `selectEstateVerdict` composing what FleetPage already selects into one full-volume sentence above the tiles — "2 services unreachable, 4 topics awaiting a move, declared and observed disagree in 3 places." | `selectors.ts`, `FleetPage.tsx` | S |
| 1.6 | **Nav reaches everything + Value renamed**: 6 destinations (Estate · Topics · Changes · Issues · Retire · Test); `#topics` route added; `#value` kept as parse alias | `App.tsx:150–189`, `routing.ts`, `ValuePage.tsx` | S |
| 1.7 | **Topics becomes a page; Topics/Topology/Recent-flows leave the Estate**. New thin `TopicsPage` (PageHead + TopicCatalog + Topology collapsed at the foot). *Recent-flows deletion is NEW, not yet ruled — PO question 2 below.* | `FleetPage.tsx:271–314`, new page | S–M |

After Wave 1 the Estate is: verdict sentence, KPI strip, disagree block, Needs attention, Contract
changes, Services — six bands, first line actionable. The critic's counted defects (twin 43-word
paragraphs, 3 of 5 preview cards non-actionable) are all downstream of 1.4–1.7.

## Wave 2 — honesty and clarity sweep — **DONE** (benzene-ui `e54351c`, 2026-08-20)

All nine landed. Two findings beyond what the review reported: the contrast failure was **all three**
light-theme RAG colours, not just red (amber 2.95:1, green 3.18:1), now 4.96/5.07/5.20 with a test
that computes the ratios; and the hover class is frozen by an architecture ratchet at 45 (down from
87) rather than burned down case by case, because the last two rounds flagged it and it regrew.

| # | Move | Why | Size |
|---|---|---|---|
| 2.1 | **Chip discipline on measurement rows**: failures take warn/bad tone, provenance becomes unpilled "via tempo" text, absences take the absent look. Today "18% of calls failed", "measured by tempo" and "error rate not reported" are pixel-identical neutral chips (`EdgeList`, `ServiceUsage`, `TopicLiveStrip`) | A reader cannot tell the alarming fact from the caveat — uniformity is the lie | S |
| 2.2 | **R9 purge**: METHOD_RAG (GET=green, DELETE=red) and event/msg badges → neutral mono chips; service/team names never wear status colour (`FleetPage.tsx:171,185` paints identities amber) | Red/amber/green mean status again, product-wide | S |
| 2.3 | **Merge the two verdict families** (`.bz-schema-mark` retires into `.bz-verdict`); lifecycle flags always Badge, verdicts always the glyph-pill — the topics STATUS column stops mixing two vocabularies in one look | One meaning, one look | S |
| 2.4 | **Absence grammar**: `EmptyState` grows to five fixed tones — empty / empty-and-good / unknown-unwired / not-yet(loading) / could-not-read — and ~9 one-off empty classes retire into it. Fixes the real bug: `data-kind="degraded"` is emitted by FleetPage and styled by nothing | "Nothing there" vs "nobody looked" vs "read failed" at a glance; R1 | M |
| 2.5 | **Glossary + Keyline mechanism + title lint**: coded terms defined once, rendered as one muted key line per card foot; architecture test fails any `title` whose text is nowhere visible; datum-carrying titles promoted into rows, definition-carrying ones to the keyline | Kills the 87-hover problem as a class (R6); survives screenshots and print | M |
| 2.6 | **schemaMismatch interim honesty**: the badge's meaning leaves the tooltip — one visible sentence in the Topic Ends card; catalogue badges drop their `title` (the explanation is one click away, R2). Full fix later: aggregator emits differing paths → straight into the existing `SchemaTree` annotations machinery, no new component | The last "detection with no finding" of the drift species | S now, M (dotnet) later |
| 2.7 | **R7 string fixes**: the retry banner's raw `benzene:mesh:query:fleet answered undefined`; "collector"/"aggregator" in reader-facing copy; the bare `dependency` resolution-hint key gets reader words | The 3am screenshot reads as English | S |
| 2.8 | **WCAG fix**: light-theme `breaking` badge to ≥4.5:1 | Measured 3.63:1 at 10.5px | XS |
| 2.9 | **Changes-page volume**: covered rollouts lose the filled red (glyph+outline stays — R4); the ~100 words of design rationale between verdict and evidence demote to keyline/half-volume | Loudness ranks by who-must-act (R8) | S |

## Wave 3 — structure and data — **DONE** (benzene-ui `d2e2093`, 2026-08-20)

All seven landed. The environment work went further than 3.5 planned: the .NET host gained a
`UseMeshUi(..., environment:)` parameter, so the seam is wired end to end rather than UI-only — but
it stays null by default and the page says "environment not published" until `placement.environment`
reaches the spec (E1), because an unlabelled production mesh rendering "dev" is the one outcome that
must not happen. The stylesheet dedupe and type scale are now enforced by tests rather than
described. 541 UI tests green, 456 .NET.

| # | Move | Why | Size |
|---|---|---|---|
| 3.1 | **Shared usage table, per-transport at topic grain**: extract `ServiceUsage`'s table, feed from a by-transport selector — lights up the Topic Traffic card AND gives RetirementRow its "over which transports" evidence | One component closes an aim-4 promise on two screens; data already in the store | S |
| 3.2 | **Topic page: three "what changed" surfaces → one card** (ContractChanges + VersionCompatibility + Since-the-previous-run under one "What changed" card; field marks stay on the Contract tree) | Same event told three times in three costumes | M |
| 3.3 | **Retire page adopts the card grammar** (tiers become Cards) | The pattern the user says works, applied | S |
| 3.4 | **Token scale + dedupe**: 27 font sizes → 7 tokens, 4 card paddings → 2, and delete the six class families tokens.css defines twice (`.bz-stats`, `.bz-page-head`, `.bz-svc-head`, `.bz-brand`, `.bz-app-head`, `.bz-divergence` — later block silently wins today) | The file stops fighting itself; every later change gets cheaper | M–L |
| 3.5 | **Environment seam (E9) + honest chip**: one-entry environment-source registry around `resolveUrl`; chrome chip reading "environment: not published" until `placement.environment` lands in the spec (E1 — the spec change is the real unblock, separate repo) | The estate on screen becomes identifiable; unknown never renders as dev | M |
| 3.6 | **Two §4 exclusions stated on-surface** (Issues foot: paging lives in your alerting tool; Traffic: ages not series) | Stops the two most re-asked questions | XS |
| 3.7 | One liveness treatment (four looks → one visible qualifier); minimal print stylesheet | Consistency; the audit artifact prints | S |

## Protect (do not touch while doing any of the above)

The honesty machinery all three reviewers singled out as genuinely good: "— not computed — the
live plane is not answering" instead of zeros; the five-sentence empty states on Issues; the
floor-not-a-total footnote; ages on every timestamp; the Service Contract card; the window
provenance on Traffic. None of the failures above require touching any of it.

## Decisions needed from the product owner

1. **Rename target for Value**: "Retire" (recommended — nav-short) or "Retirement candidates"?
2. **Delete the Estate's Recent-flows band** (1.7)? Not covered by an existing ruling. Recommended:
   delete — failing flows already surface via Needs attention, and flow-browsing is a per-subject
   activity served on Service/Topic. Fallback: demote to the Topics page.
3. **schemaMismatch full fix** needs the aggregator to publish the differing paths (approved §5.5,
   benzene-dotnet): in scope for this wave, or interim copy only?
4. **Ship the environment chip before the spec lands** (3.5)? Recommended: yes — "not published" is
   the honest third state, and E9 says the seam goes in at N=1 regardless.

## Sequencing

Wave 1 as one release (it is the visible de-messing). 2.1–2.3 + 2.7–2.8 next (small, high-clarity).
2.4/2.5 as their own PRs (mechanism changes). Wave 3 follows demand: 3.1 and 3.6 anytime; 3.4
before any further visual work compounds on the duplicated classes; 3.5 in step with the
`placement.environment` spec change.
