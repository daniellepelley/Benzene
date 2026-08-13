# Website information architecture — reducing cognitive load

**Status:** living. Phase 0 is partly done and Phase 1's docs-hub rebuild has shipped — see the
implementation log (§12). Everything else remains a proposal.
**Scope:** the public site ([benzene.app](https://benzene.app)) — its layering, navigation, visual
language, page templates, and the generator changes needed to support them. Some of the work lands in
`benzene-dotnet`, not this repo; that is called out where it applies.
**Related:** `work/repo-split-plan.md` (why the site is multi-source), `website/CLAUDE.md` (how the
generator works), `.claude/agents/cold-developer.md` (the instrument used to test it), and the
marketing planning kept in the private `benzene-admin` repo.

---

## 1. The problem, stated as a test

The content is not the problem. The specification, the .NET reference docs, the cookbooks, and the
patterns are genuinely good, and **none of it should be deleted, thinned, or hidden**. The problem is
that all of it is presented at roughly the same altitude, so a developer arriving cold has to do the
sorting themselves. The risk is that Benzene reads as large and intricate before it reads as useful,
and the visitor leaves.

Two acceptance tests, which the rest of this document is designed against:

> **The 2-minute test.** A developer who has never heard of Benzene can, within one to two minutes of
> landing, say in their own words what Benzene does and whether it applies to them.
>
> **The 5-minute test.** That same developer believes — with justification — that they could have
> their first Benzene service running within five minutes, and has an unambiguous, single link to
> start doing it.

Both are *confidence* tests, not comprehension tests. Passing them does not require simplifying
Benzene. It requires deciding what a first-time visitor is *not* shown.

---

## 2. Evidence: two cold-developer walkthroughs

Rather than reason from the maintainer's intuition, the site was **built locally and walked by a
simulated first-time visitor** (`.claude/agents/cold-developer.md`, §10). Two entry paths were run
against the same build — 117 pages, four sources, `--dotnet-docs` against a `benzene-dotnet`
checkout.

| Run | Entry point | 2-minute test | 5-minute test | Would try it? |
|---|---|---|---|---|
| **A** — front door | `index.html` | **PARTIAL** — understood at ~90s, *but not from the hero* | **PARTIAL** — 1 click to code, but no belief in 5 minutes to *running* | MAYBE, leaning yes |
| **B** — deep link from search | `docs/index.html` | **FAIL** — at 2 minutes still could not say what category of thing Benzene is | **PARTIAL** | YES, but "I nearly didn't get there" |

> Run B was re-run after the docs-hub rebuild and now **passes the 2-minute test at ~40 seconds**
> (§10, §11). The findings below are the baseline that drove the work, kept as written.

The headline result: **the site orients you well if you come through the front door and barely at all
if you don't** — and search engines don't use the front door.

### 2.1 The Docs hub is the single worst page on the site, and the nav points at it

Both runs converged on this independently. Run B, which landed there cold:

> "It tells me to start with 'what Benzene is' and then **never says what Benzene is.** Not on this
> page. Not in one sentence, not in ten words. The word 'Benzene' appears four times and not once
> attached to a definition. I don't know if this is a framework, a message broker, a spec body, a
> cloud product, or a build tool."

The hub (`RenderDocsHubPage`, `Layout.cs:292`) opens *"Start with what Benzene **is** — the
language-neutral material below — then drill into the language you build in"*, then renders 26 links
across the specification, guides and patterns before reaching **"Pick your language"**, last. Among
the first things a newcomer sees: "Porting Guide", "Port Quality Standards — the Definition of Done
for a language port", "Conformance Fixtures".

Run A, who found the hub *last*, was blunt about what would have happened otherwise:

> "If I had clicked 'Docs' first — which is what half of visitors do — I'd have concluded this was an
> academic spec project and closed the tab inside 30 seconds."

Run B also noted the .NET card sells itself by its size — *"83 pages"* — and called that "a
deterrent, not an invitation".

This ordering is correct for the spec's actual audience (people implementing a port or verifying
conformance). It is exactly inverted for everyone else, and the top-nav "Docs" link and the hero's
second button both point at it.

### 2.2 The hero defers the payoff; the page below it rescues the page above it

Run A got there, but not from the hero:

> "It did **not** come from the tagline. It came from the 'Mix transports without the glue' card plus
> the hexagon diagram plus the ten-line code sample — i.e. from scrolling past the hero."

The tagline asks the reader to hold *hexagonal*, *ports-and-adapters*, *message-driven* and *topic*
before showing any code — and **"topic", the noun the entire routing model hangs on, is not defined
anywhere on the landing page.** Run B confirmed it is first defined three pages in, on
`getting-started.html`.

Meanwhile the most persuasive sentence on the site is sitting in card #1 of section #2:

> "Serverless ties your logic to its trigger — an SNS function can't also take SQS, and putting a
> queue in front of an HTTP service is bespoke plumbing."

Run A: *"That is a real thing that has annoyed me. That's the sentence that should be the tagline."*

**"Ports" is also overloaded on the first screen** — `ports-and-adapters` in the tagline, then
`Ports: .NET` in the hero strip meaning *language implementations*, about forty words apart.

### 2.3 One recommendation the evidence corrected

An earlier draft of this document criticised the hero's `Get started` CTA for being an on-page anchor
to a code snippet rather than a tutorial. **Run A explicitly praised it:**

> "**Time to first pasteable code: one click.** The hero's 'Get started' is an anchor to a code block
> on the same page. That's excellent and I want to say so plainly. … Do not change this."

The anchor is not the problem. The problem is that the *label* promises starting and delivers
reading, and there is no second button that delivers starting. The fix is therefore additive, not
corrective: **rename the anchor to "See the code" and add a real "Start building"** pointing at the
quickstart. Recorded here because it is the clearest illustration of why the cold-developer agent
earns its keep — the intuition was wrong in a way that would have removed something good.

### 2.4 Eleven pages are called "getting started", and the five-minute claim isn't true

In `benzene-dotnet/docs/`: `getting-started.md`, `-aws`, `-aspnet`, `-google`, `-kubernetes`,
`-cloudflare`, `-grpc`, `-kafka`, `-rabbitmq`, `-worker`, `-templates`, plus `azure-functions.md`.
When eleven pages are "getting started", none of them is *the* start.

`getting-started.md` itself is good and **should be protected** — both runs said so, and Run B
credited it with converting them (*"the handler is the asset; the host is a detail"*). The problem is
what it routes *to*:

| Page | Lines | Claims |
|---|---|---|
| `getting-started-aspnet.md` | 226 | "in about five minutes" |
| `getting-started-aws.md` | 572 | recommended as the best first tour |
| `azure-functions.md` | 1,027 | the Azure entry point |
| `hosting.md` | 574 | |

Run A, on the recommended default:

> "The AWS path wants the .NET 10 SDK, an AWS account, the AWS CLI, the SAM CLI, one Benzene package
> plus three `Microsoft.Extensions.Configuration` packages, three more test packages, a handler, a
> `StartUp`, a `Function`, and a hand-written `template.yaml`, ending in `sam deploy --guided`.
> That's a good afternoon-quality tutorial. It is not five minutes, and the landing page's 'The
> quickstart is five minutes' set me up to expect otherwise."

And on being asked to choose a platform at all:

> "I'm evaluating, not deploying. I don't have a platform yet — I have a laptop."

Meanwhile the genuinely fastest path — `dotnet new` project templates — is a mid-sentence link below
the platform table.

### 2.5 Reference detail has leaked into the navigation layer, and visitors see it

`benzene-dotnet/docs/index.md` is the source of truth for the .NET sidebar. Line 74 is a **single nav
bullet containing ~1,200 words of CLI reference prose** — every flag of `benzene build`/`spec`/`diff`,
their precedence rules and exit codes — inline, for a page (`docs/cli.md`) that does not exist yet.

This is not invisible. Run B hit it and lost trust:

> "'CLI Reference (TODO, Phase 4 — `docs/cli.md` not written yet)' followed by a ~350-word unbroken
> paragraph about `--fail-on`, 'Phase 3b'… This is somebody's internal changelog pasted into a public
> docs index. It cost me trust — if this got published, what else is half-finished?"

The same index mixes altitudes freely: "Getting Started" sits four bullets from "Sampling Strategies",
"Privacy & Data Handling" and "Mesh Usage Feed", all under one **General** heading.

### 2.6 The four-language claim the site cannot currently cash

Run A tracked this across three pages and ranked it the #2 trust problem:

> "Meta description: 'implemented in .NET, Go, TypeScript, and Python.' Hero: 'implemented as
> idiomatic ports. Pick your language below' — below is a hero line reading 'Ports: **.NET**'.
> Get-started: a tab strip with **one tab**. Docs hub: one language card. … The hedge exists but it's
> always *after* the confident version. Nothing else on the site dented my trust; this did, three
> times."

The generator is already careful here — `MarketingContent.Languages` is filtered to the ports a given
run actually built, precisely so a missing port doesn't produce dead links. But the *prose* around the
selector is written for the finished state, so when the filter removes three tabs the copy keeps
promising them. The mechanism is right; the copy needs to degrade with it.

### 2.7 Page furniture that manages long documents is missing

No search, no breadcrumbs, no in-page "on this page" table of contents, no "next steps" chain. Against
a corpus with several 500–1,000-line pages, each absence is felt. Run B, arriving mid-corpus from a
search engine, had no way to tell where they were or what the site even was.

**The sidebar is also part of this.** `RenderNavNode` emits every node of a source's nav tree
expanded, always — for the .NET source that is roughly 110 links down the left of every page. The
third walkthrough (§11) stopped using it as navigation entirely:

> "On `getting-started.html` the page is telling me to make one simple choice while the left third of
> the screen shows me Kinesis, Cosmos Change Feed, Multi-Tenancy, Sagas, and 'Deprecations &
> removals'. I stopped using the sidebar — which is why I nearly missed the one cookbook that was
> written for me."

Collapsing nav groups to the active branch is a no-JS `<details>` change in the generator (the same
technique the language switcher already uses), and belongs with the Phase 4 furniture.

### 2.8 Defects the walkthroughs surfaced

These are ordinary bugs, not IA. Worth fixing regardless of what happens to the layering.

1. **The site build is currently broken against `benzene-dotnet` main.** Generation fails the
   broken-link self-check on two dangling anchors from `docs/index.md:74` (that CLI paragraph) into
   `client-sdks.html`: it links `#controlling-the-generated-namespace-with-namespace` and
   `#scoping-generation-with-topics`, but the GitHub-flavoured slugs of those headings contain a
   triple hyphen (`…-with---namespace`, `…-with---topics`) because the heading text ends in
   `` `--namespace` ``. A local build with the anchors patched succeeds and produces 117 pages, so
   this is the only thing blocking a deploy.
2. **The hero's "View on GitHub" points at the wrong repository for the code beside it.** It targets
   `daniellepelley/Benzene`, while the snippet above it and every code link in the docs live in
   `benzene-dotnet`. Run A: *"If I'd clicked the hero button expecting to find the `[Message("greet")]`
   code I'd just read, I'd have been in the wrong repo."*
3. **Two contradictory wiring models are documented, and one page explicitly denies the other.**
   `getting-started-aws.md` says in bold *"Notice there is **no Benzene registration in
   `ConfigureServices`** — no `AddBenzene()`, no `AddMessageHandlers()`"*, while the
   `aspnet-with-sqs-and-sns` cookbook **and the landing-page snippet** both open with
   `builder.Services.UsingBenzene(x => x.AddMessageHandlers(...))`. Run B: *"I cannot tell which is
   the current API… that's the kind of ambiguity that makes me suspect the framework changed shape
   mid-life and the docs didn't all get the memo."* This is the one finding that is a **product**
   question, not a docs question, and it should go to the .NET port owners.
4. **Topic separators are inconsistent within a single page** — `hello:world` and `order:placed`
   versus `orders.created` and `order.created` on `getting-started-aws.md`. Ten seconds wondering
   whether the punctuation is significant is ten seconds not spent adopting.
5. **`UseMessageHandlers()` scans every loaded assembly by default**, and the scoped alternative is
   offered mid-way through a nine-line paragraph about something else. Run A: *"the default is the one
   that'll show up in someone's cold-start bug report."*

6. **The Lambda runtime is documented two contradictory ways.** `getting-started-aws.md` says
   *"dotnet8 is the current managed runtime and works fine for a net10.0 project"*; the
   `aspnet-with-sqs-and-sns` cookbook says *".NET has no managed Lambda runtime, so the function
   ships as a self-contained executable on `provided.al2023`"*. Found on the third walkthrough:
   *"I no longer trust the deployment sections, and I'd have to go read the repo to settle it."*

Items 1 and 3–6 are `benzene-dotnet` issues; item 2 is in this repo.

### 2.9 What both runs said works — protect these

Not padding. Knowing what to protect matters as much as knowing what to fix.

- **The hexagon diagram.** *"It told me what Benzene is faster than any sentence on the site."*
  *"That did more work than the tagline did."* The one picture on the site is also, per both runs, one
  of its most effective elements. See §6 — the conclusion is *more* diagrams, not different ones.
- **One click to pasteable code** from the landing page (§2.3).
- **`getting-started.md` and `getting-started-aws.md`.** Both runs rated the AWS page the best on the
  site. *"Empty folder to deployed Lambda… the `[Message]` + `[HttpEndpoint]` pairing that answers the
  SQS-and-HTTP question in one code block and one sentence."*
- **The in-memory test host.** Both runs independently identified `BenzeneTestHost` as the single
  strongest argument for adopting Benzene over hand-rolling — *"the part I genuinely wouldn't want to
  build"* — and both noted it is three pages deep. **It deserves to be on the landing page.**
- **Honesty about maturity.** `-alpha until 1.0`, "Pre-1.0, and candid about it", "Cloudflare
  *(experimental / community)*", and the docs' willingness to flag sharp edges (*"**Unsafe by
  default:** … SNS never retries it"*). Run A: *"That bought more trust than the four marketing cards
  combined."* Run B: *"This made me **more** likely to try it, not less."*
- **The live demos being real applications rather than screenshots.**
- **The per-source sidebar.** *"Once I was inside the .NET docs I always knew where I was. The hub is
  the problem; the docs it leads to are not."*

---

## 3. Prior art: Microsoft Learn, and Dapr

> **Sourcing note.** Direct browsing of `learn.microsoft.com` and `dapr.io` is blocked by this
> session's egress policy, so this section is assembled from web-search summaries of the Azure
> developer landing page, the Azure Functions "Get started" hub, the Microsoft Learn
> contributor/style guidance, the Dapr overview/concepts/getting-started docs and the Dapr "Learn"
> hub, combined with prior knowledge of both sites. The patterns below are stable, long-standing
> features of each; anyone re-checking specific wording should open the pages directly.

Microsoft Learn is the model for **structure** — how a very large corpus stays navigable. Dapr is the
closer comparison for **positioning**: it occupies almost exactly Benzene's space (portable
abstractions over the messaging and infrastructure primitives of any cloud), it faces the same
explaining problem, and it is a good deal better at solving it visually.

### 3.1 A named content-type taxonomy, applied consistently

Every Learn article is one of a small set of declared types — **Overview, Quickstart, Tutorial,
Concept, How-to guide, Reference, Troubleshooting** — and the type is visible in the title
("Quickstart: Create your first C# function…"), in the URL, and in the left-nav grouping. The reader
knows the shape and the cost of a page before opening it. This is the highest-leverage idea on the
whole site, and it costs nothing in content: it is a labelling and grouping decision.

### 3.2 The Quickstart is a contract, not a genre

A Learn quickstart has a fixed skeleton — one sentence of what it is, **Prerequisites**, **numbered
steps**, a verification step, **Clean up resources**, **Next steps** — and a hard promise attached
("create and deploy your first functions in less than five minutes"). It covers exactly one path and
defers everything else. Alternatives are handled with **tabs** rather than a wall of sibling pages.

### 3.3 A decision page sits *above* the quickstarts

"Get Started with Azure Functions" exists purely to route. Benzene already has this in
`getting-started.md`, and both cold runs liked it. It is undermined only by what it routes into (§2.4).

### 3.4 The landing page is cards, not prose

The Azure developer landing page groups entry points as scannable card sets along several orthogonal
axes — by **scenario**, by **language**, by **tool**. A visitor self-selects on whichever axis they
already know something about. Very little continuous prose above the fold.

### 3.5 Progressive disclosure as page furniture

Breadcrumbs, a right-rail "In this article" TOC, a "Next steps" block on every article, and site-wide
search. Collectively: no page is a dead end, and no long page must be read linearly.

### 3.6 What *not* to take from Learn

Learn is enormous, and some of its properties are consequences of that scale rather than virtues:
heavy chrome, deep nav trees, aggressive versioning selectors, and near-duplicate pages generated per
language × per tool. Benzene's site is small, fast, no-JS, and one hand-written stylesheet — that is
an asset. **The goal is Learn's layering discipline, not its weight.**

### 3.7 Dapr: one picture carries the entire product surface

Dapr's signature image is a single layered diagram — a row of labelled capability tiles (service
invocation, state management, pub/sub, bindings, actors, workflow, secrets, configuration…) on a bar
representing the Dapr sidecar and its APIs, on a row of hosting environments (Kubernetes, VMs, edge,
any cloud). A visitor grasps *what the product is and how much of it there is* in about three seconds,
before reading a word.

That image does the job Benzene's landing page currently asks four feature cards and ~500 words of
prose to do. The equivalent Benzene picture is not hard to describe — **many transports in, one
handler, many hosts under** — and both cold runs asked for it by name (§6.5, D1).

### 3.8 A diagram opens every concept page, and the prose narrates it

Dapr's convention is near-mechanical: a concept page opens with a diagram and the next sentence is
literally *"The diagram below is an overview of how…"*, then walks it step by step. Service
invocation, pub/sub, building blocks, components, the sidecar — each gets one, all in the same visual
grammar (your app as one box, the sidecar as another, the infrastructure component as a third).

The reader learns the grammar once and reads every subsequent diagram for free. This is a
*convention*, not an art project, and it is the single most copyable thing on the site.

### 3.9 Diagrams use concrete named examples, not placeholders

Dapr's pub/sub diagram is a **cart** service publishing to topics that **shipping** and **email**
services subscribe to — not "Service A", "Service B". The example carries the semantics: you
understand *why* two services would want the same message without being told.

Benzene's diagram labels its centre "Your Message Handlers" and its outer nodes with host names.
Nothing in it is a worked example, so it illustrates a *shape* rather than a *scenario*.

### 3.10 Branded, collective vocabulary

"Building blocks" is a memorable collective noun that lets Dapr say "here is everything Dapr does" in
two words. Benzene's vocabulary is larger and more overlapping: *handler, topic, message, transport,
adapter, port, binding, host, middleware, pipeline, result, invocation, mesh, profile*. Several are
near-synonyms at different altitudes (transport / adapter / binding; host / platform), **and "port"
carries two unrelated meanings on the first screen** (§2.2).

This is worth a deliberate pass: not renaming the model, but deciding **which five words a first-time
visitor is allowed to meet**, and holding the rest back until the concept pages.

### 3.11 The learning path is an explicit, named choice

Dapr's "Learn" hub offers three paths and says what each is for: **Docs** (concepts), **Quickstarts**
(runnable, in .NET, Java, Python, JavaScript and Go), and **Dapr University** (a guided course in a
browser sandbox, no local setup). The visitor picks a *mode of learning*, not a *topic*.

Two things follow. First, more evidence for a distinct Start layer (§4.5). Second, the quickstarts are
**runnable repositories** — clone and run one command — not prose to be followed. Benzene already has
`examples/**` in `benzene-dotnet`; the quickstart should lead with "clone this and run it" and keep the
write-it-yourself walkthrough as the second option. Nothing beats a working service in 60 seconds for
the 5-minute test, and it neatly answers Run A's *"I don't have a platform yet — I have a laptop."*

### 3.12 Credibility signals, and Benzene's honest substitutes

Dapr leans hard on social proof: CNCF graduation, named adopters, scale claims. Benzene is pre-1.0 and
can claim none of it, and shouldn't try. But the *absence* of any credibility signal matters to a cold
visitor deciding whether this is a real project or someone's weekend framework.

Benzene's honest substitutes are already on the site and are under-sold: a **language-neutral
specification** with **conformance fixtures**, **MIT**, and — best of all — **two live demos you can
click without signing up**, which both runs noted are *real applications, not mock-ups*. The "Try it
live" section is currently the fifth section down, described in text, with no picture of what you
would be opening. The **candour about maturity** (§2.9) is itself a credibility asset and should be
kept exactly as it is.

### 3.13 What *not* to take from Dapr

Dapr's marketing home page is prose-heavy below the hero, its docs live on a separate domain
(`docs.dapr.io`) creating a visible seam between "site" and "docs", and its versioned-docs selector
adds chrome a pre-1.0 project does not need. Benzene's single-domain, single-stylesheet site is
simpler than both, and should stay that way.

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
| **0 — Landing** | `index.html` | Anyone, first 120 seconds | What it is, one diagram, one snippet, one button |
| **1 — Start** | `/start/**` (new) | A developer with an editor open | First service running in 5 minutes |
| **2 — Learn** | guides, patterns, per-language concepts & how-tos | A developer building something real | How to do the next thing |
| **3 — Reference** | the specification, package/middleware/attribute/result/config references, conformance fixtures, porting guide, capability matrix | Implementers, evaluators, port authors | Complete and precise, by design |

Layer 3 is explicitly allowed to be dense. The spec should **not** be simplified — it should be
**correctly labelled** and stop being the first thing a newcomer meets. Saying so on the page ("You
need this if you're implementing a port or verifying conformance — you don't need it to build a
service") converts intimidation into reassurance.

### 4.2 Header

From six items to five, with the developer path named and first:

```
Benzene    Start    Docs    Why Benzene    GitHub
```

- **Start** → `/start/` (Layer 1). The primary CTA everywhere on the site points here.
- **Docs** → the hub, rebuilt (§4.4) — Layers 2 and 3.
- **Why Benzene** → the existing `why.html`, with **Architecture** and **Operations** demoted to
  sub-pages linked from it, from the home page's "Built for production" cards (which already link
  them), and from the footer. Strong evaluator pages; they do not need two of six top-nav slots
  competing with the developer path.
- **GitHub** → must point at the repo matching the code the visitor just read (§2.8, item 2).

### 4.3 Home page

Same content, re-ordered, with three copy fixes:

1. **Hero.** Lead with "One message handler, every transport." Move *hexagonal / ports-and-adapters /
   message-driven* out of the tagline into "The core idea" below, where the diagram gives them
   something to attach to. Promote the line both cold runs picked out — *"an SNS function can't also
   take SQS"* — into or immediately under the tagline. Resolve the **"ports" collision** (§2.2): the
   hero strip should say "Languages:", not "Ports:". Two CTAs: **Start building** (→ `/start/`) and
   **See the code** (the existing anchor, per §2.3).
2. **Show the code immediately** — promote the language-tab snippet to directly under the hero.
3. **Add the test host to the landing page.** Both runs independently named `BenzeneTestHost` the
   strongest reason to adopt Benzene, and both found it three pages deep. Four lines of it in a
   feature card, or as a second tab beside the handler snippet.
4. **The core idea** (diagram) → **Why Benzene?** cards → platforms → **Try it live** (now with
   screenshots, §6.5 D4) → **Built for production**.
5. **Make the language copy degrade with the filter** (§2.6): when a run builds one port, the prose
   should say ".NET today; Go, TypeScript and Python in progress" rather than "pick your language
   below" above a single tab. Drive the wording from the same filtered list the selector uses.

### 4.4 Docs hub — the highest-priority fix on the site

Both cold runs identified this page as the thing most likely to lose a visitor. Rebuild it:

1. **One sentence saying what Benzene is, at the very top** — the definition Run B never got. The
   `benzene-dotnet` index already has a serviceable one: *"Benzene is a hexagonal framework designed
   for services running in serverless environments, containers, or on physical servers…"* Run B:
   *"It's the word 'framework' that unlocked it — that one noun told me more than the entire hub."*
2. **The hexagon diagram, right under it.** It already exists; it is simply on the one page a
   deep-linked visitor never sees. Run B: this alone *"would have single-handedly fixed the 2-minute
   failure."*
3. **"Pick your language" first**, as cards, .NET marked as the reference implementation. Describe the
   card by what it gets you, not by page count — never "83 pages".
4. **A link to Start**, prominently.
5. **Learn** — guides and patterns, grouped by what they help you *do*, not alphabetically.
6. **The specification last**, under a heading stating who it is for and explicitly saying a service
   author does not need it. Group by the spec's own two-part structure (Core Specification / Cloud
   Service Profile) rather than the current flat alphabetical list.

### 4.5 The Start section (the only genuinely new content)

A new cross-language `/start/` section in *this* repo:

- **`start/index.md`** — the router. Adapted from `benzene-dotnet/docs/getting-started.md`, which both
  runs praised, promoted to the site's front door and made language-aware.
- **`start/<language>/quickstart.md`** — one per port, each a strict quickstart per §5.1. Hard
  ceiling: **150 lines and five steps.**
- **Lead with clone-and-run.** Per §3.11 and Run A's *"I have a laptop"*: step one is cloning a
  runnable example and running one command. The write-it-yourself walkthrough is the second option on
  the same page, and the `dotnet new` templates — currently a mid-paragraph link below the fold —
  belong at the top.
- **The recommended default changes.** An undecided developer goes to the **local HTTP host** (ASP.NET
  Core for .NET, each port's equivalent) — no cloud account, no credentials, no deploy. It is the only
  path that can honestly claim five minutes. The AWS "one function, every event source" story is the
  best *demonstration* of why Benzene matters, so it becomes the **first "Next step"**, framed as "now
  make it interesting" — not the on-ramp.
- **Fix the five-minute claim** wherever the real path is longer (§2.4). An honest "about 20 minutes,
  and you'll have it deployed" beats a false five.

The existing long guides are **not** deleted or shortened. `getting-started-aws.md` and
`azure-functions.md` are reclassified as Layer 2 "Deploy to X" how-to guides, keep their URLs, and are
what the quickstart's Next-steps points at. This is why the recommendation is cheap: the expensive
content already exists and stays where it is.

### 4.6 Page-type labels, applied corpus-wide

Adopt the Learn taxonomy, trimmed: `overview · quickstart · tutorial · concept · how-to · reference ·
troubleshooting`. Declared per page (§7), rendered as a badge next to the title, and used to group the
sidebar.

This is the change that lets ~120 existing pages be sorted into layers **without rewriting any of
them**, and it makes the layering self-enforcing: a page that cannot be given one type is a page
trying to be two things — exactly the `azure-functions.md` failure mode.

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
copy-pasteable; **the time claim in the title must be true**; ends by pointing forward.

`getting-started-aspnet.md` (226 lines) is already ~80% of this and is the natural first conversion.

### 5.2 Every article, every layer

- **One opening sentence** stating what the page is and who it's for.
- **Breadcrumb** — `Docs / .NET / Hosting`.
- **"On this page"** TOC for any page over ~200 lines. This alone transforms `azure-functions.md`.
- **"Next steps"** at the foot — always. No dead ends.
- **Type badge** next to the title.

### 5.3 Reference (Layer 3)

The opposite treatment: dense, complete, tabular, no narrative. State at the top that this is
reference and link to the concept page that explains it. The five existing `docs/reference/*` pages in
`benzene-dotnet` are already in this shape; the CLI content stranded in `index.md:74` becomes the
missing `docs/reference/cli.md`.

---

## 6. Visual language: the whole site has one diagram

### 6.1 The measurement

Counted on the local build (117 pages, four sources):

- **Image files in the output: two** — `favicon.svg` and `og-image.svg`. Neither is a diagram; the OG
  card is the social-sharing thumbnail.
- **`<img>` tags in the entire site, excluding the vendored demos: zero.**
- **Inline `<svg>` elements:** `index.html` has three — header logo, hero logo, hexagon. **Every other
  page on the site has exactly one: the header logo.**

So: **one diagram, on one page.** All 96 .NET documentation pages, all eleven specification pages, the
guides, the patterns and the docs hub have no picture of any kind. Most pointedly, **the page called
"Architecture" contains no architecture diagram.**

For a project whose core proposition is a *shape*, that is the largest unforced gap on the site.

### 6.2 The one diagram works — which is the argument for more of them

The temptation is to critique the hexagon. The evidence says don't: both cold runs named it among the
most effective things on the site (§2.9). Whatever its theoretical flaws, it out-performed every
sentence in the hero.

The real findings are about **coverage and claim**:

- **It is on one page**, and not the one a deep-linked visitor lands on. Run B: putting it on the Docs
  hub *"would have single-handedly fixed the 2-minute failure."*
- **It illustrates hosts, not transports.** Both runs, independently, asked for a *different* picture
  — *"API Gateway, SQS, SNS, EventBridge arrows into a single Lambda box… The landing-page hexagon
  shows hosts, which is a different claim."* The hexagon answers "where does it run"; the pitch is
  "how many things can call it". Both are worth drawing; only one is drawn.
- **It illustrates the etymology.** Its own source comment says so: *"literally six sides for six
  adapters, tying the 'hexagonal architecture' name back to the shape itself."* That explains why the
  pattern has its name to a visitor who has not asked. It is a fine diagram for the Architecture page,
  next to prose that uses the word.
- **Nothing in it is concrete** — no topic, no message, no payload (§3.9).

Keep it. Add to it.

### 6.3 The mechanism is already proven

The hard part is done. `ArchitectureDiagram` generates inline SVG from C#, and `site.css` themes it
with the same custom properties as everything else:

```css
.arch-diagram line  { stroke: var(--border); }
.arch-diagram .core { fill: var(--accent); fill-opacity: 0.12; stroke: var(--accent); }
.arch-diagram .node { fill: var(--bg); stroke: var(--accent); }
```

Light/dark for free, no asset pipeline, no binary blobs in git, crisp at any zoom, text selectable,
and `role="img"` + `aria-label` already present. **What is missing is coverage and a stated grammar,
not technology.**

### 6.4 A visual grammar, decided once

Borrowed from Dapr's discipline (§3.8): the reader learns the vocabulary from the first diagram and
reads every later one for free.

- **Shape carries role.** Rounded rectangle = *your code*. Square = *Benzene*. Chip/pill = *a transport
  or event source*. Cylinder = *someone else's infrastructure* (queue, broker, database).
- **Direction is always left-to-right** — request in on the left, result out on the right. Arrowheads
  on every edge; no undirected lines.
- **Colour never decorates.** Accent means "the part Benzene owns"; muted means "yours" or "theirs". A
  reader who cannot see colour loses nothing, because shape and position already carry it.
- **Concrete examples only.** `order:placed`, an `orders` queue, `POST /orders` — never "Service A".
  (And pick one topic separator first — see §2.8, item 4.)
- **Every diagram is narrated.** The paragraph immediately after walks it in order.
- **Every diagram is accessible.** `role="img"` and an `aria-label` stating the *content*, as the
  existing one already does.
- **Wide diagrams scroll inside their own container** (`.arch-diagram-wrap` already does this).

### 6.5 The diagram inventory

Ordered by value per unit of effort. D1–D3 were each requested, unprompted, by both cold runs.

**D1 — "One handler, every transport" (landing page + docs hub).** The picture the site is missing.
Left: inbound chips — `POST /orders`, an `orders` SQS queue, an `orders` Kafka topic — arrows
converging on **one** box: `[Message("order:placed")] PlaceOrderHandler`. Below it, a strip of the
hosts that box runs in. One image, and the tagline stops needing *hexagonal*, *ports-and-adapters* and
*message-driven* to make its point.

**D2 — Before / after (`why.html`, and possibly the landing page).** Two panels. *Without:* three
separate functions, each with its own copy of validation, logging, auth, separately deployed. *With:*
one service, one pipeline, three bindings. Run A asked for exactly this and called it "the comparison
I arrived making, and the site never draws it". It requires no Benzene vocabulary to read, which makes
it the most persuasive image available to someone still deciding whether to care.

**D3 — The request lifecycle (concepts, and the quickstart).** Left-to-right: native event → adapter →
envelope (topic / headers / body) → middleware chain (correlation → validation → auth → retry) →
handler → result → native response. The one picture that defines *transport*, *topic*, *middleware*,
*handler* and *result* simultaneously — the exact five words that currently arrive as undefined prose.
Run B: *"would have defined topic and middleware for me on the landing page instead of on page three."*

**D4 — Screenshots of the two live demos ("Try it live").** The cheapest item here: two PNGs. Both
runs flagged the Mesh UI as a differentiating claim they had no picture of. Per §3.12 the demos are
Benzene's strongest honest credibility signal. (The one place raster images are right — an SVG mock of
a UI would misrepresent what you get.)

**D5 — Per-concept diagrams in the same grammar**, one each on core concepts, wire contracts,
transport bindings, middleware, and — not least — the **Architecture** page, which currently has none.
Layer 2/3 work, sequenced after the layering lands.

**D6 — Expected terminal output in the quickstart.** Not a diagram, but the same instinct: show the
reader what success looks like so they can tell whether they have it.

> Worth noting: the `aspnet-with-sqs-and-sns` cookbook already contains a plain-ASCII box-and-arrow
> chain in a `<pre>` block, and Run B singled it out as explaining the architecture faster than the
> two paragraphs around it. Even the cheapest possible diagram is beating the prose. That is the whole
> argument for this section, already proven inside the corpus.

### 6.6 The one mechanical decision to make

Marketing pages can keep generating diagrams inline from C#. **Docs pages cannot** — they are
Markdown, and post-split they live in four other repositories, so they cannot call into the generator.

So the shared diagrams need publishing as standalone files at a stable site path
(`/assets/diagrams/*.svg`) that any source's Markdown can reference, generated from the same C# that
renders them inline. The wrinkle: `SiteBuilder.RewriteLinks` resolves image links against the crawled
page set by disk path, and `WebAssetExtensions` vendors images found next to the Markdown that
references them — so a root-absolute href to a generator-emitted diagram is a case neither path
handles today. Small change, but decide it before D5 rather than during it.

---

## 7. Generator changes

All in `website/generator`. No new dependencies, no JS framework, no build step.

| # | Change | Files | Size |
|---|---|---|---|
| 1 | Header nav: 6 → 5 items, add **Start**; fix the GitHub target | `Layout.cs` (`Header`) | trivial |
| 2 | Home page: reorder, promote snippet, two CTAs, "Languages:" not "Ports:", filter-aware language copy | `Layout.cs`, `MarketingContent.cs` | small |
| 3 | **Docs hub rebuild**: definition sentence + hexagon + language-first ordering | `Layout.cs` (`RenderDocsHubPage`) | small — *highest priority* |
| 4 | New `start` `DocSource` (cross-cutting, `IsLanguage=false`, prefix `start`) | `Program.cs` | small — follows the existing `guides`/`patterns` pattern exactly |
| 5 | New diagrams D1–D3 + emit shared SVGs to a stable path (§6.6) | new `Diagrams/*.cs`, `SiteBuilder.cs`, `site.css` | medium |
| 6 | Page-type front matter → badge + nav grouping | `SiteBuilder.cs` | medium — **note:** `UseAdvancedExtensions()` does *not* include YAML front matter, so the pipeline needs `.UseYamlFrontMatter()` (`SiteBuilder.cs:15`), or the type comes from an index-file convention instead |
| 7 | "On this page" TOC from `h2`/`h3` (headings already carry GitHub-slug ids from `AssignHeadingIds`) | `Layout.cs`, `site.css` | medium |
| 8 | Breadcrumbs from the source + nav path | `Layout.cs` | small |
| 9 | "Next steps" block rendered from front matter | `Layout.cs`, `SiteBuilder.cs` | medium |
| 10 | Site search: emit `search-index.json` + ~60 lines of vanilla JS | `SiteBuilder.cs`, `Layout.cs`, `site.css` | medium |

Two existing invariants make this safe and should be preserved: the **broken-link self-check** (which
catches every stale link and dangling anchor the reshuffle creates — it is already earning its keep,
§2.8) and the rule that **nav is derived from each source's own index file**, so most reordering is
content edits, not code.

---

## 8. Phasing

Ordered by value per unit of effort. Each phase is independently shippable.

**Phase 0 — Unblock and de-bug.** Fix the two dangling anchors so the site builds at all (§2.8, item
1); fix the GitHub link so it points at the repo the code beside it lives in; raise the wiring-model
contradiction (item 3) with the .NET port owners. Hours, not days.

**Phase 1 — Signposting (this repo only, no new content).** Docs hub rebuild first — it is the page
both cold runs said would lose them. Then header nav, home-page reorder, tagline and "Ports" fixes.
Delivers most of the 2-minute-test improvement. *Caveat: **Start** points at the existing
`getting-started.html` until Phase 3.*

**Phase 2 — Diagrams D1–D3, and the demo screenshots (D4).** Deliberately early, ahead of the Start
section: the cold runs show pictures carrying more of the explaining load than any copy change, and D4
is two PNGs. D1 on both the landing page *and* the docs hub.

**Phase 3 — The Start section.** `start/index.md` + a .NET quickstart under §5.1, leading with
clone-and-run, defaulting to the local HTTP host. This is what makes the 5-minute test pass. Other
ports follow as they firm up.

**Phase 4 — Page furniture.** "On this page" TOC, breadcrumbs, "Next steps". Biggest win for the
existing long pages, with no content rewriting.

**Phase 5 — Page types.** Front matter + badges + type-grouped sidebars. Requires a pass over every
doc index across all port repos, so it wants the earlier phases proven first.

**Phase 6 — Search.** Highest value for return visitors; lowest for the two acceptance tests.

**Cross-repo:** Phases 0 and 4–6 touch `benzene-dotnet` (and eventually the other ports) because that
is where the doc content and index files live. Generator changes ship first and stay
backward-compatible — a page with no declared type renders exactly as today — so repos can be brought
along one at a time without breaking a build. The `docs/index.md:74` CLI paragraph →
`docs/reference/cli.md` extraction is a `benzene-dotnet` change and can happen any time.

---

## 9. Explicit non-goals

- **No content is deleted or thinned.** The reference depth is Benzene's differentiator against
  frameworks that are easy to start and impossible to operate.
- **The specification is not simplified.** It is relabelled and moved down the hub; that is all.
- **The candour is not softened.** Both cold runs named honesty about pre-1.0 status and sharp edges
  as a *reason to trust the project*. Do not let a polish pass sand it off.
- **No JS framework, no npm, no Node build step.** The search index is static JSON plus vanilla JS;
  everything else is generated HTML and one hand-written stylesheet.
- **No audience-labelled pages.** The existing value/theme framing is deliberate and kept; the layering
  here is by *depth*, which is orthogonal to it.
- **No URL breakage without redirects.** The generator already writes redirect stubs.

---

## 10. How we'll know it worked

**The primary instrument is `.claude/agents/cold-developer.md`**, added alongside this document. It
simulates a developer meeting Benzene for the first time, walks the built site under a real visitor's
constraints — landing page first, click only what you'd click, never read the source, give up when
you'd give up — and reports in the first person against both acceptance tests. It is deliberately the
inverse of `dx-champion`: it knows nothing, fixes nothing, and its only value is the accuracy of its
ignorance.

Run it against `website/dist` after every phase, and vary the persona — the two runs recorded here
(front door, and deep link from a search engine) produced *different* verdicts on the same build,
which is precisely the point. Other personas worth running: a tech lead evaluating for a team, a
non-.NET developer, and someone arriving on a deep reference page.

The baseline is recorded in §2: **PARTIAL / PARTIAL / MAYBE** through the front door, **FAIL /
PARTIAL / YES-but-barely** from a deep link. The target is PASS / PASS from both.

**First measured movement.** Re-running the deep-link persona against the rebuilt docs hub (§11)
moved the 2-minute test from **FAIL to PASS**, and the visitor now understands Benzene *before*
leaving the hub:

> "It clicked on the landing page itself, at the bolded lede plus the hexagon diagram, inside about
> 40 seconds. That is a real achievement for a page that isn't the front door. Critically, the docs
> hub avoids 'hexagonal' and 'ports-and-adapters' entirely — **landing there was better than landing
> on the home page would have been.**"

That last clause is the strongest available argument for the Phase 1 home-page work: the hub's
plain-language definition now outperforms the hero it was modelled to support. The 5-minute test
stayed PARTIAL, exactly as predicted — it is Phase 3 that moves it, because the remaining gap is that
no path on the site produces a running service without an AWS account.

Secondary measures:

- **Path length to first code.** Today: one click from the landing page (good, protect it); **five
  clicks, four of them choices, from the "Docs" nav item**. Target: one click from either.
- **Time to *running* code.** Today: not five minutes on the recommended path, and the site says
  otherwise. Target: a true claim, whatever the number is.
- **Quickstart conformance.** Every Layer 1 page fits §5.1 (≤150 lines, ≤5 steps, has Prerequisites /
  Verify / Next steps). Mechanically checkable in CI.
- **No dead ends.** Every published page has a "Next steps" block — checkable in the same pass as the
  existing broken-link self-check.
- **Diagram coverage.** Every Layer 0–2 landing/concept page has at least one diagram. Today: one page
  out of 117.
- **Analytics** (GA4 already wired, consent-gated): the home → Start → quickstart funnel, and bounce
  rate on the docs hub. Directional only, and only once traffic means anything.

---

## 11. Implementation log

Dated entries. Everything not listed here is still a proposal.

### 2026-08-13 — Phase 0 (partial) and the Phase 1 docs-hub rebuild

**Shipped in this repo:**

- **Docs hub rebuilt** (`Layout.RenderDocsHubPage`) per §4.4 — the highest-priority item on the list,
  and the page both cold runs said would have ended their visit:
  - opens with a **definition sentence** naming the noun ("Benzene is a framework for message-driven
    services") and the payoff, with no *hexagonal*, *ports-and-adapters* or *topic* in it;
  - carries the **architecture diagram**, which previously appeared only on the landing page a
    deep-linked visitor never sees;
  - a **"Start building in .NET"** button directly under the definition;
  - **"Pick your language" moved from last to first**, with each card offering "Start here →" and
    "Browse the docs" separately, and the .NET card tagged *reference*. Cards no longer advertise a
    page count — "83 pages" read as a deterrent;
  - **Guides and patterns** next (material for people *using* Benzene);
  - **the specification last**, under a lede that opens *"You don't need this to build a service"*
    and says who does.
  - The per-language start link reuses `MarketingContent.Languages[].DocsOutputPath` — already the
    source of truth for the home page's selector — with a fallback to the source's docs home, so a
    language wired only via `--source` can't emit a link to a page that was never generated.
- **The get-started panel now links the language's own repo** (§2.8, item 2). The header's GitHub
  link points at the cross-language home, which is not where the snippet beside it lives. This also
  fixes a pre-existing duplicate: the non-beta branch linked the same docs page twice.
- **CSS:** `.hub-cta`, and `.card-tag` (an accent-coloured positive marker, as opposed to `.beta`'s
  muted warning).

Build verified locally: 117 pages, four sources, broken-link self-check clean.

**Verified by re-running the persona that failed.** The deep-link run went **2-minute test: FAIL →
PASS**, clicking at ~40 seconds on the hub itself (§10). Two things it then flagged were fixed in the
same pass:

- **"Pick your language" over a single card read as a broken page** — *"being asked to 'pick' from a
  set of one made me wonder whether the page was broken"*. The heading and lede now degrade with the
  same filter the cards do: with one port wired it reads "Build it in .NET" and states plainly that
  the other ports are early and will appear as their docs land. This was already §4.3 item 5; the
  hub needed it first.
- **Nothing on the hub said the project is pre-1.0**, which cost it a 2-minute-test deduction even
  though the candour exists elsewhere on the site and both earlier runs called it a *reason to
  trust* Benzene. A one-line note now sits under the CTA.

Left for later, as scoped: the home page's tagline is now demonstrably worse than the hub's
(Phase 1), and the ~110-link always-expanded sidebar (§2.7) is Phase 4.

**Not done — blocked on repository access:**

- **The two dangling anchors that currently fail the site build** (§2.8, item 1) are in
  `benzene-dotnet/docs/index.md`, which this session can read but not push to. **The website deploy
  is red until someone fixes them:** the links should be
  `client-sdks.md#controlling-the-generated-namespace-with---namespace` and
  `client-sdks.md#scoping-generation-with---topics` (triple hyphen — the headings end in
  `` `--namespace` `` / `` `--topics` ``). Everything above was verified against a locally patched
  checkout.
- **The wiring-model contradiction** (§2.8, item 3), the topic-separator inconsistency (item 4) and
  the `UseMessageHandlers()` default (item 5) are all `benzene-dotnet` issues. The first is a product
  question for the .NET port owners, not a docs edit.

## 12. Sources

**Microsoft Learn / Azure**
- [Azure developer documentation](https://learn.microsoft.com/en-us/azure/developer/)
- [Get Started with Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-get-started)
- [Quickstart: Create your first C# function in Azure using Visual Studio](https://learn.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio)
- [Get started with the Azure Quickstart Center](https://learn.microsoft.com/en-us/azure/azure-portal/azure-portal-quickstart-center)
- [Microsoft Learn content and resource types](https://learn.microsoft.com/en-us/training/support/learn-content-types)
- [Microsoft Learn style guide — Quick start](https://learn.microsoft.com/en-us/contribute/content/style-quick-start)
- [Reference documentation — Microsoft Style Guide](https://learn.microsoft.com/en-us/style-guide/developer-content/reference-documentation)

**Dapr**
- [Dapr — Distributed Application Runtime](https://dapr.io/)
- [Dapr overview](https://docs.dapr.io/overview/)
- [Building blocks concept](https://docs.dapr.io/concepts/building-blocks-concept/)
- [Components concept](https://docs.dapr.io/concepts/components-concept/)
- [Dapr sidecar (daprd) overview](https://docs.dapr.io/concepts/dapr-services/sidecar/)
- [Service invocation overview](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/service-invocation-overview/)
- [Publish and subscribe overview](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/)
- [Getting started with Dapr](https://docs.dapr.io/getting-started/) · [Quickstarts](https://docs.dapr.io/getting-started/quickstarts/)
- [Learn Dapr: Docs, Quickstarts, and Dapr University](https://dapr.io/learn/)
