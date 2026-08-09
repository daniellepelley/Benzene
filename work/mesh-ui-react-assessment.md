# Mesh UI — assessment: single-file HTML → React + TypeScript component library

**Status:** Assessment for decision. No code written, nothing committed to.
**Date:** 2026-08-08
**Scope:** `mesh-ui/mesh-ui.html` and its siblings, and what a componentised React rewrite would cost.

---

## 1. What exists today

`mesh-ui/mesh-ui.html` is **5,037 lines / 274 KB in one file**:

| Part | Lines |
|---|---|
| `<style>` | 708 (261 CSS classes) |
| `<script>` | 4,000 (181 functions) |
| Markup | ~330 |

It is written in pre-ES6 idiom — **181 `function` declarations and zero arrow functions** — and drives the
DOM directly: **97 `getElementById`**, 56 `addEventListener`, 25 `innerHTML` assignments, and **595
top-level `let`/`var` bindings**. There is no render boundary and no module system. `renderFleetPage`
alone is 204 lines.

**It has zero external dependencies.** No CDN script, no stylesheet link, no font. This is not an
oversight and it is the single most important fact in this document — see §3.

### It is not one file, it is four copies of two-and-a-bit UIs

| File | Lines | Where |
|---|---|---|
| `mesh-ui.html` | 5,036 | `Benzene/mesh-ui/` |
| `mesh-ui.html` | 5,036 | `benzene-dotnet/src/Benzene.Mesh.Ui/` (byte-identical) |
| `index.html` | 5,036 | `Benzene/website/demos/mesh/` (byte-identical) |
| `mesh-spec-ui.html` | 955 | `benzene-dotnet/src/Benzene.Mesh.Ui/` |
| `spec-ui.html` | 1,317 | `benzene-dotnet/src/Benzene.Spec.Ui/` |

The three *distinct* UIs share only **30 CSS class names across all three** (`badge`, `chip`,
`brand`, `empty-state`, `copy-btn`…). There is a real shared design language; it is currently
propagated by copy-paste. Go, TypeScript and Python have no UI at all yet — so "cross-language
concern" is today an aspiration, not a fact.

## 2. Why it gets buggy when you change it

The instinct is right, and the causes are specific and measurable:

1. **No isolation.** 595 top-level bindings in one scope. A name introduced for the fleet view is
   visible to the issues view. Nothing tells you which of the 181 functions reads a given global.
2. **No render boundary.** State changes are applied by locating a node (`getElementById`) and
   mutating it. Whether the screen matches the data after an edit is a property of *every* code path
   that touches that node, not of one function.
3. **String-built HTML.** 25 `innerHTML` assignments mean markup correctness is unchecked — there is
   no compiler and no type between a data shape and the DOM it produces.
4. **CSS is one 261-class global namespace** shared with the other two UIs by duplication. Renaming or
   restyling has no blast-radius boundary.
5. **There are no tests of the UI's behaviour — at all.** The only test file
   (`benzene-dotnet/test/Benzene.Mesh.Test/MeshUiMiddlewareTest.cs`, 6 assertions) verifies the
   *middleware that serves the file*: path matching, HTTP method, trailing slashes. Not one line of
   the 4,000 lines of JavaScript is exercised. Every change is verified by opening it and looking.

That last point is the one worth sitting with. The rewrite's value is not React; it is that **4,000
lines of untested logic become testable**.

## 3. The constraint that shapes the whole design

`mesh-ui.html` is an **`EmbeddedResource`** in `Benzene.Mesh.Ui.csproj`, read at runtime via
`GetManifestResourceStream` and written straight to the response by `MeshUiMiddleware`. A Benzene
service serves its own dashboard from inside its own process, with **no CDN, no network egress, no
build step and no static-file hosting** at the consumer.

That property is worth more than the current implementation is. Any rewrite must still produce **one
self-contained artifact** — HTML with JS and CSS inlined, no external requests. React can do this
(Vite plus a single-file plugin inlines everything), but it constrains the whole plan:

- No code splitting, no lazy routes, no runtime CDN anything.
- Bundle size becomes a budget, not an afterthought. Today's artifact is 274 KB uncompressed. React
  plus React-DOM is ~135 KB minified before any application code, so a naive rewrite lands *larger*.
  Preact (~10 KB, React-compatible via aliasing) is the obvious lever and should be evaluated early
  rather than retrofitted.
- The build output must be committed or produced in CI and vendored, because `dotnet pack` needs the
  file present. **This is the biggest workflow change in the whole proposal** — see §7.

## 4. The component inventory already exists

The file has already been factored along component lines; it just has no framework to express it. There
are **35 `render*` / `build*` functions**, which map almost one-to-one onto a component tree:

**Pages** — `renderFleetPage`, `renderServicePage`, `renderTopicPage`, `renderIssuePage`,
`renderComposePage`

**Sections** — `renderTopology`, `renderTopologyGraph`, `renderIssues`, `renderTopics`,
`renderHealthChecks`, `renderFeedHealth`, `renderValueView`, `renderSchemaTree`, `renderThread`,
`renderServiceAbout`, `renderTopicRows`, `renderFleetInto`,
`buildServiceTopicsSection`, `buildServiceTopologySection`, `buildServiceUsageSection`,
`buildTopicPayloadSection`, `buildTopicServiceSection`, `buildTopicUsageSection`,
`buildTopicPageVersionSection`, `buildVersionCompatibilitySection`, `buildDiscussionSection`

**Controls / primitives** — `buildServiceCard`, `buildIssueRow`, `buildValueRow`, `buildUsagePanel`,
`buildServiceLiveStrip`, `buildTopicLiveStrip`, `buildServiceTopicList`, `buildServiceEdgeList`,
`buildComposer`

Plus the design primitives currently expressed only as CSS classes and shared with the other two UIs:
badge, chip, chip-group, RAG status glyph, empty state, copy button, brand header, chevron/disclosure.

**This is the answer to "a team could build their own mesh UI".** The primitives and the section
components are the publishable surface; the five pages are one opinionated assembly of them.

## 5. Data layer

Eight `fetch` call sites, all resolved through a single `resolveUrl` helper against a manifest root:
`services/{name}.json`, `topics.json`, `topology.json`, `usage.json`, `annotations.json`, plus two
POST paths for the composer/discussion and one generic JSON GET.

These shapes are already specified — `docs/specification/mesh.md` and the `Benzene.Mesh.Contracts`
types. **Generating TypeScript types from the spec rather than hand-writing them is the highest-value
piece of the data layer**, because it makes contract drift a compile error in the UI instead of an
`undefined` at runtime. It also gives the other ports something to conform to.

## 6. Proposed shape

```
mesh-ui/                        (stays in this repo — cross-language, per work/README.md)
  package.json  tsconfig.json  vite.config.ts
  src/
    primitives/     Badge, Chip, StatusGlyph, EmptyState, CopyButton, Disclosure, BrandHeader
    controls/       ServiceCard, IssueRow, ValueRow, UsagePanel, LiveStrip, Composer, TopicList
    sections/       Topology, TopologyGraph, Issues, Topics, HealthChecks, FeedHealth, SchemaTree, …
    pages/          Fleet, Service, Topic, Issue, Compose
    data/           generated contract types, fetch clients, polling hooks
    theme/          design tokens (the 30 shared classes become the shared layer)
  stories/          one story per primitive/control/section
  test/             vitest + Testing Library
  dist/mesh-ui.html single self-contained artifact — the thing that ships
```

**Stack:** TypeScript, Vite, `vite-plugin-singlefile`, Vitest + Testing Library, Storybook 8.
**Rendering:** React API, but evaluate Preact-via-alias at the first bundle measurement (§3).

## 7. What this actually costs

Ordered by risk, not by sequence. Sizes are relative; treat them as shape, not schedule.

| # | Work | Size | Notes |
|---|---|---|---|
| 1 | **Toolchain from zero** | M | Neither repo has *any* JS/TS tooling — no `package.json`, no `tsconfig`, no npm in CI. This is a new capability to own: lockfiles, dependency updates, supply-chain review, a Node version in CI. It is the real cost of the decision, and it is permanent. |
| 2 | **Single-file build + packaging** | M | Prove `dist/mesh-ui.html` embeds and serves byte-identically before writing components. Decide committed-artifact vs CI-built (§8). |
| 3 | **Design tokens + primitives** | S | 7-ish primitives, the 30 shared classes. Unlocks the other two UIs later. |
| 4 | **Contract types from the spec** | S–M | Generate from `docs/specification/mesh.md` fixtures / `Benzene.Mesh.Contracts`. |
| 5 | **Controls + sections** | L | ~30 components. The bulk, but mechanical once 3 and 4 land. |
| 6 | **Topology graph** | M | `renderTopologyGraph` + `nodeWidth` is hand-rolled SVG layout — the one piece that is genuinely algorithmic rather than presentational. Port as-is first; do not redesign it during the migration. |
| 7 | **Pages + routing** | M | Five pages; hash routing (`#fleet`, `#service/…`, `#topic/…`, `#issue/…`, compose). |
| 8 | **Tests** | M | The point of the exercise. Target the logic that has never been tested: issue collection, staleness, RAG derivation, windowing. |
| 9 | **Storybook** | S | Cheap once components exist. Publishable to the website as a static build. |
| 10 | **Fold in the other two UIs** | M | `mesh-spec-ui` and `spec-ui` rebuilt on the shared primitives. **Explicitly out of scope for phase 1** — but the primitives should be designed knowing it is coming. |

**Overall: a substantial piece of work, not a weekend.** The honest framing is that items 1 and 2 are
the decision, and items 3–9 are execution that can be staged.

## 8. Decisions needed before starting

1. **Committed artifact or CI-built?** `dotnet pack` needs `mesh-ui.html` present. Either the built
   file is committed (simple, but a generated file in git and a diff on every change) or CI builds it
   and the .NET package consumes it (clean, but couples the .NET release to a Node build). **This is
   the one that must be settled first** — everything else follows from it.
2. **React or Preact?** Same API; ~125 KB of budget difference on a 274 KB baseline.
3. **Where does the source live?** Recommendation: this repo, since the UI is a cross-language concern
   and `mesh-ui/` is already here. Each port then vendors `dist/mesh-ui.html`, exactly as
   `Benzene.Mesh.Ui` does today.
4. **Is the duplication fixed as part of this?** There are four copies today, one byte-identical. The
   same "one home per document" rule that was just applied to `work/` applies here — the build should
   produce one artifact and every consumer should vendor it, not hold a hand-edited copy.
5. **Does Storybook get published?** A static Storybook on benzene.app is the strongest possible
   demonstration of "build your own mesh UI", and is nearly free once components exist.

## 9. Risks

- **Bundle size regression.** Mitigated by measuring at step 2, before any component is written.
- **Behaviour drift during the port.** There are no tests to port against, so "does it still work" is
  currently a human looking at it. Suggest capturing the current UI's rendered output against the
  `website/demos/mesh/` fixtures as approval snapshots *before* starting — that is the only safety net
  available, and it is cheap.
- **The toolchain is permanent.** Adding npm to a project that has deliberately avoided it is a
  one-way door. Worth being deliberate rather than incidental about it.
- **Scope creep into redesign.** The temptation to improve the UI while porting it will be strong. The
  topology graph (item 6) is where this will hurt most.
