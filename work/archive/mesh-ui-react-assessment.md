> ARCHIVED 2026-08-20: actioned — decision executed; benzene-ui is the React + TypeScript component library.

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
  **React is settled** (maintainer ruling, 2026-08-08: nothing else is as well known or as well
  supported, and that matters more than kilobytes for a component library other teams are meant to
  adopt). So the budget is spent, not saved — plan for the artifact growing, keep the *application*
  code lean, and measure at step 2 rather than at the end.
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
**Rendering:** React. Settled — see §3.

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

1. ~~Committed artifact or CI-built?~~ **Settled 2026-08-09: committed, with a freshness check.**
   `benzene-ui` commits its build output to `build/mesh-ui.html`, and its CI fails if a fresh rebuild
   does not reproduce those bytes exactly. The alternative — every consumer running `npm ci` — would
   put a Node toolchain in the critical path of a NuGet package build, for the sake of obtaining an
   HTML file. The usual objection to committing a build output is silent drift; the byte check
   removes it, and the build is deterministic, which is what makes the check meaningful.
2. ~~React or Preact?~~ **Settled: React.**
3. ~~Where does the source live?~~ **Settled: its own repo,
   [benzene-ui](https://github.com/daniellepelley/benzene-ui)** — see §10, which argued this against
   the original recommendation of keeping it here.
4. ~~Is the duplication fixed as part of this?~~ **Settled: yes.** All three vendored copies
   (`mesh-ui/mesh-ui.html`, `website/demos/mesh/index.html`,
   `benzene-dotnet/src/Benzene.Mesh.Ui/mesh-ui.html`) are now byte-identical build outputs of the
   same source, and the hand-written page is retired.
5. **Does Storybook get published?** A static Storybook on benzene.app is the strongest possible
   demonstration of "build your own mesh UI", and is nearly free once components exist. **Still
   open** — it builds in CI but nothing publishes it yet.

### What the port actually found *(2026-08-09)*

Three things a componentisation was not expected to surface, all of which were defects in the
shipped page rather than in the port:

- **The live-plane shape was invented.** The first cut of the React store held a friendly
  `{heartbeats, flows}` snapshot that appears nowhere in Benzene. The real contract is `FleetView`,
  and it carries three honesty channels that shape had nowhere to put: `missingFeeds` (a dimension
  the plane genuinely cannot supply, so render "—" not the non-nullable 0), `window.countsWindowed`
  (the counts answer a different window than the flows), and an *absent* `lastSeen` (no live-time
  signal, which is not staleness). Storing the wire contract rather than a projection of it is now
  a rule in the library's own guide.
- **A parity sweep was necessary and was not free.** Eight of the original's `render*`/`build*`
  functions had no counterpart at the point the port looked finished — the value/retirement view,
  version compatibility, per-service usage, the service's self-description, and the feed-health
  line among them. Roughly 85% parity looks like 100% from the outside.
- **The service card's `raw` and `health` links carry an XSS guard.** They come from a self-reported
  manifest, and `target="_blank"` does not neutralise a `javascript:` href. It is one line, it is
  easy to lose in a rewrite, and it is now a test.

## 10. Where should it live? — the repo question

**Recommendation: a separate repository. Do not put it in the specification repo.**

### The evidence

**1. This repo has no JavaScript toolchain at all, and that is deliberate.** Its three CI workflows
(`deploy-website.yml`, `promote-website.yml`, `sync-test-environment.yml`) check out sibling repos,
run a **C# generator**, and sync to S3. There is no `package.json`, no `setup-node`, no npm anywhere.
Adding React, Storybook and Vitest makes this a two-toolchain repository with two CI paths and two
dependency ecosystems.

**2. The dependency surface is the strongest argument.** React + Storybook + Vitest pulls in several
hundred transitive npm packages, a lockfile, Dependabot traffic and a supply-chain review obligation
— into the repository whose entire job is to hold the **normative specification**. `docs/specification/`
should be boring, stable and trustworthy. A spec repo with a churning `node_modules` dependency graph
is a worse spec repo, even if nothing actually breaks.

**3. Release cadence is mismatched, in both directions.** The spec changes rarely and deliberately;
every change ripples to four language ports. A component library changes constantly — dependency
bumps, component fixes, Storybook upgrades. Coupling them means routine npm patch releases churn the
repository that holds the contract, and spec-version tags stop meaning anything about the UI.

**4. The distribution goal requires it.** "A team could build their own mesh UI out of the components"
means publishing an npm package (`@benzene/mesh-ui` or similar). That is natural from a dedicated
repo and awkward from the spec repo.

**5. The cross-repo contract problem is already solved here.** The obvious objection — "but the
contracts live in the spec" — has an existing answer. Every language port already vendors
`docs/specification/conformance/*.json` with a recorded `SPEC_VERSION` and a CI drift check against
this repo. A UI repo would use **exactly that mechanism** to generate its TypeScript types from
`mesh-descriptor-cases.json`, `mesh-trace-cases.json`, `mesh-issue-cases.json` and
`mesh-collector-cases.json`. This is not a new coupling to invent; it is the pattern the project
already runs, and it makes contract drift a build failure in the UI.

**6. It matches the split that already happened.** Spec / .NET / Go / TypeScript / Python / admin are
separated by concern. A browser UI with its own toolchain is another distinct concern, not an
exception.

### The argument for keeping it here, and why it loses

`mesh-ui/` is already in this repo and the website's `demos/mesh/` uses it. But that copy is
*vendored verbatim* by `SiteBuilder.CopyDemos()` — it is already consumed as a build artifact, not as
source. Vendoring a released `dist/mesh-ui.html` from another repo is the same operation the website
already performs, and the same one `Benzene.Mesh.Ui.csproj` performs when it embeds the file.

The genuine cost is one more repo to coordinate. That is real but small, and it is the cost the
project has already accepted five times.

### On the name — the one thing worth reconsidering

**`benzene-mesh` is the wrong name for this**, because the mesh is not the UI. In .NET alone the mesh
is **twenty packages**: `Benzene.Mesh.Collector`, `.Aggregator`, `.Contracts`, `.Discovery.Aws`,
`.Discovery.Azure`, `.Discovery.Kubernetes`, `.Fleet.Aws.XRay`, `.Fleet.Jaeger`, `.Fleet.Tempo`,
`.Usage.CloudWatch`, `.Usage.ApplicationInsights`, `.Wire`, `.Dispatch`, `.Reporting`, storage
adapters, and `.Ui`. Those backends stay per-language, as you say. A repository called `benzene-mesh`
that contains only a React component library will read, to anyone arriving at the org, as *the mesh* —
and they will go looking for the collector in it.

Two better options:

- **`benzene-ui`** — my preference. There are **three** UIs, not one: `mesh-ui.html` (5,036 lines),
  `mesh-spec-ui.html` (955) and `spec-ui.html` (1,317), and they already share 30 CSS class names by
  copy-paste. A single component library that all three are built from is the coherent end state, and
  `spec-ui` is not mesh — so a mesh-named repo would be wrong for a third of its contents from day one.
- **`benzene-mesh-ui`** — precise, unambiguous, and correct if the spec UIs are deliberately left out.

The decision that matters is **separate repo: yes**. The name is secondary, but `benzene-ui` buys room
for the two spec UIs that otherwise need a home later.

### What would live there

```
benzene-ui/
  packages/components/     the publishable library: primitives, controls, sections
  packages/mesh-ui/        the opinionated assembly → dist/mesh-ui.html
  packages/spec-ui/        (later) the other two UIs on the same primitives
  contracts/               vendored conformance fixtures + SPEC_VERSION + drift check
  stories/                 Storybook, published static to benzene.app
```

Consumers are unchanged in shape: `Benzene.Mesh.Ui.csproj` embeds a released `dist/mesh-ui.html`
exactly as it embeds the hand-written one today; the website vendors the same artifact into
`demos/mesh/`. Neither needs to know a Node build exists.

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
