# Website information architecture — reducing cognitive load

**Status:** proposal, not yet actioned. Nothing in this document has been implemented.
**Scope:** the public site ([benzene.app](https://benzene.app)) — its layering, navigation, page
templates, and the generator changes needed to support them. Some of the work lands in
`benzene-dotnet`, not this repo; that is called out where it applies.
**Related:** `work/repo-split-plan.md` (why the site is multi-source), `website/CLAUDE.md` (how the
generator works), and the marketing planning kept in the private `benzene-admin` repo.

---

## 1. The problem, stated as a test

The content is not the problem. The specification, the .NET reference docs, the cookbooks, and the
patterns are genuinely good, and **none of it should be deleted, thinned, or hidden**. The problem is
that all of it is presented at roughly the same altitude, so a developer arriving cold has to do the
sorting themselves. The risk is that Benzene reads as large and intricate before it reads as useful,
and the visitor leaves.

Two acceptance tests, which the rest of this document is designed against:

> **The 2-minute test.** A developer who has never heard of Benzene can, within one to two minutes of
> landing on the home page, say in their own words what Benzene does and whether it applies to them.
>
> **The 5-minute test.** That same developer believes — with justification — that they could have
> their first Benzene service running within five minutes, and has an unambiguous, single link to
> start doing it.

Both are *confidence* tests, not comprehension tests. Passing them does not require simplifying
Benzene. It requires deciding what a first-time visitor is *not* shown.

---

## 2. Where the load comes from today

Evidence-based audit of the current site and the doc sources it stitches together.

### 2.1 The first decision is between four abstractions

The header is `Home · Why Benzene · Architecture · Operations · Docs · GitHub`
(`website/generator/Layout.cs:520`). Four of the six items are conceptual essays. **Nothing in the
top nav is named "Start", "Quickstart", or "Tutorial"** — the words a developer scans for. The
closest thing, "Docs", is the most abstract destination on the site (see 2.3).

### 2.2 "Get started" doesn't start anything

The hero's primary CTA is `Get started`, which is an on-page anchor to `#get-started`
(`Layout.cs:74`). That section is a language-tab widget showing an install line and a ~10-line
snippet (`BuildGetStartedSelector`, `Layout.cs:492`). It is a good *illustration*; it is not a start.
A visitor who clicks the biggest button on the page and gets a code sample rather than step 1 of
something has had their main call to action absorbed by a scroll.

The home page's own reading order compounds this. Sections run: hero → **Why Benzene?** (4 feature
cards) → **The core idea** (hexagon SVG) → **Get started** → **And it runs wherever you already
are** (6 platform pills) → **Try it live** (2 demos) → **Built for production, not just prototypes**
(3 more cards). The first concrete artifact — code — is the *third* section down, after roughly 500
words of positioning.

### 2.3 The `Docs` link lands new developers on spec-author material

`Docs` targets the cross-language hub (`RenderDocsHubPage`, `Layout.cs:292`), whose lede reads *"Start
with what Benzene **is** — the language-neutral material below — then drill into the language you
build in."* The hub therefore renders, in order:

1. **The specification** — a flat alphabetical `<ul>` of the normative documents: design principles,
   wire contracts, transport bindings, mesh contracts, payload schema versioning, porting guide, port
   quality standards, conformance fixtures, the Cloud Service Profile.
2. **Guides**, **Patterns**.
3. **Pick your language** — *last*.

For the intended audience of the spec (people implementing a port, or verifying conformance) this
ordering is exactly right. For a developer who wants to write a handler it is precisely inverted: the
material they need is below the fold, behind ~15 links to documents that exist to constrain
implementers. "Transport Bindings" and "Conformance Fixtures" as the second and third things a
newcomer sees is the single largest contributor to the "this is enormous" reaction.

### 2.4 Eleven pages are called "getting started"

In `benzene-dotnet/docs/`: `getting-started.md`, `-aws`, `-aspnet`, `-azure` (as
`azure-functions.md`), `-google`, `-kubernetes`, `-cloudflare`, `-grpc`, `-kafka`, `-rabbitmq`,
`-worker`, `-templates`. When eleven pages are "getting started", none of them is *the* start.

`getting-started.md` itself is a good platform router (58 lines, a decision table, "the one idea they
all share"). The problem is what it routes *to*:

| Page | Lines | Claims |
|---|---|---|
| `getting-started-aspnet.md` | 226 | "in about five minutes" |
| `getting-started-aws.md` | 572 | recommended as the best first tour |
| `azure-functions.md` | 1,027 | the Azure entry point |
| `hosting.md` | 574 | |

The only page that plausibly passes the 5-minute test is the 226-line ASP.NET one — and it is *not*
the recommended default. The router explicitly steers an undecided reader to the 572-line AWS guide
because it is the best demonstration of the value proposition. That is a reasonable
*marketing* judgement making a *first-run* judgement, and the first-run one should win: the fastest
success is a local HTTP service with no cloud account.

A 1,027-line page is not a quickstart. `azure-functions.md` is a hosting reference that has been
asked to also be an on-ramp, and it can't be both.

### 2.5 Reference detail has leaked into the navigation layer

`benzene-dotnet/docs/index.md` is the source of truth for the .NET sidebar (parsed as a bullet tree by
`NavTreeBuilder`). It carries ~60 links across 10 groups. Line 74 is a **single nav bullet containing
roughly 1,200 words of CLI reference prose** — every flag of `benzene build`/`spec`/`diff`, their
precedence rules, and their exit codes — inline, for a page (`docs/cli.md`) that does not exist yet.

The nav builder drops prose from group headers, so this mostly doesn't render — but it is the clearest
symptom of the underlying habit: when there is no layer that owns "dense reference", the detail
settles wherever it was written, including in the table of contents.

The same list mixes altitudes freely: "Getting Started" sits four bullets from "Sampling Strategies",
"Privacy & Data Handling", and "Mesh Usage Feed" under one **General** heading.

### 2.6 The page furniture that manages long documents is missing

The site has no search, no breadcrumbs, no in-page "on this page" table of contents, and no
"next steps" chain at the foot of an article. Against a corpus with several 500–1,000 line pages,
each of those absences is felt: a reader who lands mid-corpus from a search engine has no cheap way
to tell where they are, what this page is *for*, or what to read next.

### 2.7 The tagline asks for four concepts in one breath

> "One message handler, every transport. Benzene is a hexagonal (ports-and-adapters) architecture for
> message-driven services: write your logic once, against a topic, and reach it over HTTP, queues,
> streams, and serverless functions — all at once, on the cloud you already run."

The first five words are excellent and pass the 2-minute test on their own. The rest of the sentence
introduces *hexagonal*, *ports-and-adapters*, *message-driven*, and *topic* before the reader has seen
a single line of code. Each is load-bearing later; none is needed in the first fifteen seconds.

---

## 3. What Microsoft does, and which parts are worth taking

> **Sourcing note.** Direct browsing of `learn.microsoft.com` is blocked by this session's egress
> policy, so this section is assembled from web-search summaries of the Azure developer landing page,
> the Azure Functions "Get started" hub, and the Microsoft Learn contributor/style guidance, combined
> with prior knowledge of the site's structure. The patterns below are stable, long-standing features
> of Learn; anyone re-checking specific wording should open the pages directly.

### 3.1 A named content-type taxonomy, applied consistently

Every Learn article is one of a small set of declared types — **Overview, Quickstart, Tutorial,
Concept, How-to guide, Reference, Troubleshooting** — and the type is visible in the title
("Quickstart: Create your first C# function…"), in the URL, and in the left nav grouping. The reader
knows the shape and the cost of a page before opening it. This is the highest-leverage idea on the
whole site, and it costs nothing in content: it is a labelling and grouping decision.

### 3.2 The Quickstart is a contract, not a genre

A Learn quickstart has a fixed skeleton — one sentence of what it is, **Prerequisites**, **numbered
steps**, a verification step, **Clean up resources**, **Next steps** — and a hard promise attached
("create and deploy your first functions in less than five minutes"). It deliberately covers exactly
one path and defers everything else. Alternatives are handled with **tabs** (VS Code / CLI / Visual
Studio / portal) rather than a wall of sibling pages.

### 3.3 A decision page sits *above* the quickstarts

"Get Started with Azure Functions" exists purely to route: here are your options, here is which one to
pick, here is the five-minute one. Benzene already has this in `getting-started.md` — it is one of the
better pages in the corpus. It is undermined only by what it routes into (2.4).

### 3.4 The landing page is cards, not prose

The Azure developer landing page groups entry points as scannable card sets along several
orthogonal axes — by **scenario** (application hosting, consuming cloud services from existing apps,
AI apps, serverless), by **language** (.NET, C++, Go, Java, JavaScript, Python, Rust), and by
**tool**. A visitor self-selects on whichever axis they already know something about. There is very
little continuous prose above the fold.

### 3.5 Progressive disclosure as page furniture

Breadcrumbs, a right-rail "In this article" TOC, a "Next steps" block on every article, and
site-wide search. Collectively these mean no page is a dead end and no long page has to be read
linearly.

### 3.6 What *not* to take

Learn is enormous, and some of its properties are consequences of that scale rather than virtues:
heavy chrome, deep nav trees, aggressive versioning selectors, and a lot of near-duplicate pages
generated per language × per tool. Benzene's site is small, fast, no-JS, and one hand-written
stylesheet — that is an asset. **The goal is Learn's layering discipline, not its weight.**

---

## 4. Recommendation: three layers, and yes — split the site

### 4.1 Should the site be split further?

**Yes**, and the answer to "getting started vs. the deep stuff" is that it needs *three* tiers, not
two. Two tiers pushes everything that isn't a quickstart into one undifferentiated "advanced" bucket,
which is the current problem at a smaller scale.

Critically, this should be **a navigation and labelling change, not a content migration**. Almost
every existing page keeps its URL and its content. What changes is which layer owns it, where it
appears in nav, and what furniture surrounds it.

| Layer | Owns | Audience | Promise |
|---|---|---|---|
| **0 — Landing** | `index.html` | Anyone, first 120 seconds | What it is, one snippet, one button |
| **1 — Start** | `/start/**` (new) | A developer with an editor open | First service running in 5 minutes |
| **2 — Learn** | guides, patterns, per-language concepts & how-tos | A developer building something real | How to do the next thing |
| **3 — Reference** | the specification, package/middleware/attribute/result/config references, conformance fixtures, porting guide, capability matrix | Implementers, evaluators, port authors | Complete and precise, by design |

Layer 3 is explicitly allowed to be dense. The spec should *not* be simplified — it should be
**correctly labelled** and stop being the first thing a newcomer meets. Saying so on the page ("You
need this if you're implementing a port or verifying conformance — you don't need it to build a
service") converts intimidation into reassurance.

### 4.2 Header

From six items to five, with the developer path named and first:

```
Benzene    Start    Docs    Why Benzene    GitHub
```

- **Start** → `/start/` (Layer 1). The primary CTA everywhere on the site points here.
- **Docs** → the hub, reordered (4.4) — Layers 2 and 3.
- **Why Benzene** → the existing `why.html`, with **Architecture** and **Operations** demoted to
  sub-pages linked from it, from the home page's "Built for production" cards (which already link
  them), and from the footer. They are strong evaluator pages; they do not need to spend two of six
  top-nav slots competing with the developer path.

### 4.3 Home page

Same content, re-ordered and trimmed at the top:

1. **Hero.** Lead with "One message handler, every transport." as the headline claim. Move
   *hexagonal / ports-and-adapters / message-driven* out of the tagline and into "The core idea"
   section immediately below, where the diagram gives them something to attach to. Two CTAs, not
   three: **Start building** (→ `/start/`) and **Read the docs**. GitHub moves to the header only.
2. **Show the code immediately.** Promote the language-tab snippet to directly under the hero, with a
   two-line frame: *"This is a complete Benzene service. It answers on HTTP; wiring it to a queue is
   one more line."* Seeing the shape is what makes the abstractions land.
3. **The core idea** (diagram) — unchanged, now third.
4. **Why Benzene?** feature cards — unchanged, now fourth.
5. Platforms → Try it live → Built for production — unchanged.

That is one deletion (a CTA), one reordering, and one tagline edit. It moves concrete before abstract
without losing a word of the positioning.

### 4.4 Docs hub, inverted

`RenderDocsHubPage` currently leads with cross-cutting sources and ends with "Pick your language".
Invert it:

1. **Pick your language** — first, as cards, with the .NET card marked as the reference implementation.
2. **Learn** — guides and patterns, grouped by what they help you *do*, not alphabetically.
3. **The specification** — last, under a heading that states who it's for and explicitly says a
   service author doesn't need it. Group the links by the spec's own two-part structure (Core
   Specification / Cloud Service Profile) instead of the current flat alphabetical `<ul>`.

### 4.5 The Start section (the only genuinely new content)

A new cross-language `/start/` section in *this* repo, containing:

- **`start/index.md`** — the router. One question ("where does this run?"), one recommended default
  answer, a table of the alternatives. Adapted from `benzene-dotnet/docs/getting-started.md`, which is
  already close to right, but promoted to the site's front door and made language-aware.
- **`start/<language>/quickstart.md`** — one per language port, each a strict quickstart per the
  contract in §5.1. Hard ceiling: **150 lines and five steps.**
- **The recommended default changes.** An undecided developer should be sent to the **local HTTP
  host** (ASP.NET Core for .NET, and each port's equivalent) — no cloud account, no credentials, no
  deploy. It is the only path that can honestly claim five minutes. The AWS "one function, every event
  source" story is the best *demonstration* of why Benzene matters, so it becomes the **first "Next
  step"** off the quickstart, framed as "now make it interesting" — not the on-ramp.

The existing long guides are **not** deleted or shortened. `getting-started-aws.md` and
`azure-functions.md` are reclassified as Layer 2 "Deploy to X" how-to guides, keep their URLs, and are
what the quickstart's Next-steps block points at. This is why the recommendation is cheap: the
expensive content already exists and stays exactly where it is.

### 4.6 Page-type labels, applied corpus-wide

Adopt the Learn taxonomy, trimmed to what Benzene needs:

`overview` · `quickstart` · `tutorial` · `concept` · `how-to` · `reference` · `troubleshooting`

Declared per page (front matter or an index convention — see §6), rendered as a small badge next to
the page title, and used to group the sidebar. This is the change that lets ~120 existing pages be
sorted into layers **without rewriting any of them**, and it makes the layering self-enforcing: a
page that can't be given a type is a page that is trying to be two things, which is exactly the
`azure-functions.md` failure mode.

---

## 5. Page templates

### 5.1 Quickstart (Layer 1) — a contract

```
# Quickstart: <one outcome> in <N> minutes
One sentence: what you'll have at the end.
## Prerequisites          — a short list, each with a link
## 1..5 <numbered steps>  — every step has a runnable command or a paste-able block
## Verify it              — the command, and the exact expected output
## Next steps             — 2–3 cards, never a bare list
```

Rules: no more than five steps; no digressions (link out instead); every code block complete and
copy-pasteable; the time claim in the title must be true for someone with the prerequisites already
installed; ends by pointing forward.

`getting-started-aspnet.md` (226 lines) is already ~80% of this and is the natural first conversion.

### 5.2 Every article, every layer

- **One opening sentence** stating what the page is and who it's for.
- **Breadcrumb** — `Docs / .NET / Hosting`.
- **"On this page"** TOC for any page over ~200 lines. This alone transforms `azure-functions.md`.
- **"Next steps"** at the foot — always. No dead ends.
- **Type badge** next to the title.

### 5.3 Reference (Layer 3)

Reference pages get the opposite treatment: dense, complete, tabular, no narrative. State at the top
that this is reference and link to the concept page that explains it. The five existing
`docs/reference/*` pages in `benzene-dotnet` are already in this shape; the CLI content currently
stranded in `index.md:74` becomes the missing `docs/reference/cli.md`.

---

## 6. Generator changes

All in `website/generator`. No new dependencies, no JS framework, no build step — the existing
constraints hold.

| # | Change | Files | Size |
|---|---|---|---|
| 1 | Header nav: 6 → 5 items, add **Start** | `Layout.cs` (`Header`) | trivial |
| 2 | Home page: reorder sections, promote snippet, two CTAs | `Layout.cs`, `MarketingContent.cs` | small |
| 3 | Docs hub: invert section order, group the spec list, add the "who this is for" lede | `Layout.cs` (`RenderDocsHubPage`) | small |
| 4 | New `start` `DocSource` (cross-cutting, `IsLanguage=false`, prefix `start`) | `Program.cs` | small — follows the existing `guides`/`patterns` pattern exactly |
| 5 | Page-type front matter → badge + nav grouping | `SiteBuilder.cs` | medium — **note:** `UseAdvancedExtensions()` does *not* include YAML front matter, so the pipeline needs `.UseYamlFrontMatter()` (`SiteBuilder.cs:15`) or the type must come from an index-file convention instead |
| 6 | "On this page" TOC from `h2`/`h3` (headings already have GitHub-slug ids from `AssignHeadingIds`) | `Layout.cs`, `site.css` | medium |
| 7 | Breadcrumbs from the source + nav path | `Layout.cs` | small |
| 8 | "Next steps" block rendered from front matter | `Layout.cs`, `SiteBuilder.cs` | medium |
| 9 | Site search: emit `search-index.json` at generation time + ~60 lines of vanilla JS | `SiteBuilder.cs`, `Layout.cs`, `site.css` | medium |

Two existing invariants make this safe and should be preserved: the **broken-link self-check** (which
will catch every stale link the reshuffle creates, including dangling `#fragment`s) and the rule that
**nav is derived from each source's own index file**, so most of the reordering is content edits, not
code.

---

## 7. Phasing

Ordered by value per unit of effort. Each phase is independently shippable.

**Phase 1 — Signposting (this repo only, no new content).**
Header nav (Start/Docs/Why); home page reorder + tagline trim; docs hub inversion. Delivers most of
the 2-minute-test improvement for a day or two of work. *Caveat: **Start** points at the existing
`getting-started.md` until Phase 2 lands.*

**Phase 2 — The Start section.**
`start/index.md` + one .NET quickstart under the §5.1 contract, defaulting to the local HTTP host.
Wire the `start` `DocSource`. This is what makes the 5-minute test pass. Adds the other ports'
quickstarts as those ports firm up.

**Phase 3 — Page furniture.**
"On this page" TOC, breadcrumbs, "Next steps" blocks. Biggest single win for the existing long pages,
with no content rewriting.

**Phase 4 — Page types.**
Front matter + badges + type-grouped sidebars. Requires a pass over every doc index across all port
repos, so it wants the earlier phases proven first.

**Phase 5 — Search.**
Highest absolute value for return visitors; lowest for the two acceptance tests, which is why it is
last.

**Cross-repo:** Phases 3–5 touch `benzene-dotnet` (and eventually `benzene-go`, `-typescript`,
`-python`) because that is where the doc content and index files live. The generator changes ship
first and stay backward-compatible — a page with no declared type renders exactly as it does today —
so the repos can be brought along one at a time without breaking a build. The `docs/index.md:74` CLI
paragraph → `docs/reference/cli.md` extraction is a `benzene-dotnet` change and can happen any time.

---

## 8. Explicit non-goals

- **No content is deleted or thinned.** The reference depth is Benzene's differentiator against
  frameworks that are easy to start and impossible to operate. Every page in the audit above keeps
  its content.
- **The specification is not simplified.** It is relabelled and moved down the hub; that is all.
- **No JS framework, no npm, no Node build step.** The search index is a static JSON file plus vanilla
  JS; everything else is generated HTML and one hand-written stylesheet.
- **No audience-labelled pages.** The existing framing (value/theme, not job title) is deliberate and
  is kept; the layering here is by *depth*, which is orthogonal to it.
- **No URL breakage without redirects.** The generator already writes redirect stubs; anything that
  moves gets one.

---

## 9. How we'll know it worked

- **Scripted cold-developer walkthrough.** Someone unfamiliar with Benzene, timed, asked after 2
  minutes: "what does this do, and is it for you?" and after 5: "could you build something with it?"
  Run before Phase 1 as a baseline and after each phase. This is the primary measure — the acceptance
  tests in §1 are the definition of done.
- **Path length to first code.** Clicks and words from the home page to a runnable snippet. Today: one
  scroll past ~500 words, or two clicks to a 226-line guide reached via a router that recommends a
  572-line one. Target: one click.
- **Quickstart conformance.** Every Layer 1 page fits §5.1's contract (≤150 lines, ≤5 steps, has
  Prerequisites / Verify / Next steps). Mechanically checkable in CI.
- **No dead ends.** Every published page has a "Next steps" block. Also mechanically checkable, in the
  same pass as the existing broken-link self-check.
- **Analytics** (GA4 is already wired, consent-gated): the home → Start → quickstart funnel, and
  bounce rate on the docs hub. Directional only, and only after enough traffic to mean anything.

---

## 10. Sources

- [Azure developer documentation](https://learn.microsoft.com/en-us/azure/developer/)
- [Get Started with Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-get-started)
- [Quickstart: Create your first C# function in Azure using Visual Studio](https://learn.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio)
- [Get started with the Azure Quickstart Center](https://learn.microsoft.com/en-us/azure/azure-portal/azure-portal-quickstart-center)
- [Microsoft Learn content and resource types](https://learn.microsoft.com/en-us/training/support/learn-content-types)
- [Microsoft Learn style guide — Quick start](https://learn.microsoft.com/en-us/contribute/content/style-quick-start)
- [Reference documentation — Microsoft Style Guide](https://learn.microsoft.com/en-us/style-guide/developer-content/reference-documentation)
