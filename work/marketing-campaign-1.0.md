# Benzene 1.0 — Marketing Campaign Plan

**Status:** DRAFT for maintainer review — first-pass strategic shape, not finished copy
**Last Updated:** 2026-07-25
**Purpose:** A coordinated, phased campaign to take Benzene from ~zero awareness to a name a .NET
developer building cloud services has encountered from several independent directions. Blogs are
the spine; this document picks the wedge, designs the blog programme, names the channels and the
people to approach, gives a recommendation on Microsoft/AWS involvement, and states honestly what
it costs a solo maintainer in hours.

**Companions (build on, don't re-litigate):** [`work/website-marketing-aims.md`](website-marketing-aims.md)
(messaging pillars), [`work/website-audience-plan.md`](website-audience-plan.md) (audiences),
[`work/benzene-vision.md`](benzene-vision.md) (the philosophy copy must stay honest to),
[`docs/capability-matrix.md`](../docs/capability-matrix.md) (the honest boundaries),
[`work/1.0-release-plan.md`](1.0-release-plan.md) (the authoritative launch state).

---

## 1. Situation

### 1.1 Awareness is effectively zero, and that is the accurate baseline

- The GitHub repo (`daniellepelley/Benzene`) has **no tagged release** (`git tag` returns nothing),
  and `version.txt` is still `0.0.2`. Every NuGet package published to date is `-alpha`.
- There is **no blog**. `website/generator/` builds a marketing home page, value pages, docs and two
  live demos — there is no blog section anywhere in `SiteBuilder.cs`/`MarketingContent.cs`. The
  spine of this campaign currently has no home.
- The site exists and is deployed (`dev.benzene.app` on every push to `main`, promoted manually to
  `benzene.app` via `.github/workflows/promote-website.yml`). **Needs verification:** the older
  `work/website-live-assessment-2026-07-15.md` refers to a different live host
  (`www.golambda.co.uk`) and could not reach it — confirm which domain is actually serving before
  any campaign link points at it.
- Two **live demos already exist and are published** with the site (`website/demos/`): the Spec UI
  viewer (self-contained, an "Orders Service" spec fixture — read-only, the *Try it / Send* panel is
  not exercised) and the Mesh Explorer (a real topology graph over static fixtures). These are the
  only visual assets the project has. **There are no screenshots of anything, anywhere in the repo**
  (zero raster images), which matters for §5's posts and any talk.
- There is one genuinely public artefact already in the world: the maintainer's 2023 Digiterre
  experience report, *"Microservices in Serverless Functions"*, cited as the origin of the project
  in `work/benzene-vision.md`. That is a real, pre-existing, third-party-hosted credibility asset
  and the campaign should build on it rather than start from nothing.
- Git history shows **282 commits, no external contributors, and a large proportion of commits
  authored by an AI agent** (`git log --format='%an'`). This is publicly visible. See §9.

### 1.2 The product is close but not launchable today

Per `work/1.0-release-plan.md` (the authoritative, code-verified driver — the older
`1.0.0-release-status.md` and `1.0-readiness-checklist.md` are explicitly superseded and stale):

- **26 of 29 worklist items are closed**, all Tier 0/1/2/3 items included. The three open items are
  cosmetic or test-depth (`4.2` example-project naming reconciliation, `5.3` real-dependency test
  tier for Azure/Kafka, `5.4` a cross-cutting coverage matrix). **None is a marketing blocker.**
- A **release dry-run was verified on 2026-07-19**: `dotnet pack` produced 135 packages + 134 symbol
  packages, zero `NU5xxx` warnings, MIT licence metadata, SourceLink, packed READMEs. The pipeline
  *can* emit a real `1.0.0`.
- The remaining action is a **decision, not work**: bump `version.txt`, tag, publish, cut the GitHub
  release, drop the prerelease badges.

**Honest read: the product is ready to absorb attention, but the artefact a campaign points at does
not exist yet.** Today a visitor lands on a site with no blog, and a README whose install command
says `--prerelease`. Every hour of promotion spent before the tag is wasted, and worse — a "1.0
launch" that resolves to an `-alpha` package is exactly the credibility damage §9 exists to avoid.

**Three marketing-side gaps that are cheap to close and currently block a good launch:**

1. **No blog on `benzene.app`.** Prerequisite for everything in §5. Route to the website owner.
2. **No `PackageIcon`.** `1.0-release-plan.md` Tier 0.7 flags this deliberately — the mark exists
   only as SVG (`website/generator/Logo.cs`, `website/generator/assets/favicon.svg`) and there is no
   PNG anywhere in the repo. The mark itself is *good*: a hexagon with an inscribed ring — the
   chemistry shorthand for the benzene molecule *and* the shape of hexagonal architecture. It needs
   a raster export, not a design exercise. It will also be every social card and every talk slide.
3. **A live doc-truth bug sits directly under the campaign's best post.**
   `docs/testing-benzene.md` (line 49) and `docs/cookbooks/testing-lambda-functions.md` both tell
   readers that `AwsLambdaBenzeneTestHost` comes from **`Benzene.Tools`** — a package with *no
   source in `src/`*. The type actually lives in `Benzene.Aws.Lambda.Core.TestHelpers`. Post **E2**
   is built on exactly this doc, and the first thing a curious reader will do is try that install
   command and fail. **Route to the core/DX owner; must be fixed before E2 ships.**

### 1.3 What we are actually selling into

The engineering behind this is far ahead of the awareness: **155 packages** in `src/`, 24 dedicated
`*.TestHelpers` packages, a stated 1,532 passing core tests (`1.0-release-plan.md`), a draft
language-neutral specification with real JSON conformance fixtures, and a working mesh UI. Nobody
knows. That asymmetry is the whole problem this campaign exists to fix.

A niche inside a niche: .NET developers doing serverless/event-driven cloud work who feel transport
coupling as daily pain. Treat the smallness as targeting precision — we can name the exact rooms
these people are in (§6). They are also conservative framework buyers who will ask "who's behind
this, will it exist in three years, why not minimal APIs?" — the campaign must answer the trust
question, not only the feature question.

---

## 2. Objective

**Get Benzene from unknown to *considered*: a project that a .NET developer with a transport-coupling
problem has heard of from more than one direction, and that at least a handful of unaffiliated
people are actually running.**

Timeframe: **8 months from the 1.0 tag** (tag = T0).

Success, defined in advance and deliberately modest:

| By | Target | Why this number |
|---|---|---|
| T0 + 2 weeks | 2+ newsletter/aggregator pickups; front page of r/dotnet once | These are binary, cheap, and the honest test of whether the launch post is interesting |
| T0 + 3 months | 15+ GitHub issues/discussions opened by **people who are not the maintainer** | The only early signal that distinguishes reading from trying |
| T0 + 6 months | **3 unaffiliated people using Benzene in something real** — a repo, a talk, a blog post, a production service they mention | The metric that actually matters (§8) |
| T0 + 8 months | 1 podcast episode, 1 user-group talk, 1 vendor-blog placement delivered | Proof the 360° surfaces actually turned on |

Realistic honest ceiling for a campaign this size, run by one person: **roughly 300–600 developers
genuinely engage** (read a post to the end, click through), **30–60 run the quickstart**, **5–10
build something**, **1–3 keep it**. Anyone promising a hockey stick from a .NET framework launch in
2026 is selling something.

---

## 3. Audience & wedge

### 3.1 Audience, in priority order

Building on `work/website-audience-plan.md`, ranked for *this* campaign:

1. **The .NET developer on a serverless/event-driven team** (audience A). They discover, they try,
   they advocate upward. Every evergreen post targets them. **~70% of campaign effort.**
2. **The architect / tech lead** (audience B). They approve. They ask "will this box us in?" and
   read `docs/capability-matrix.md` before anything else. **~20%.**
3. **DevOps/SRE** (audience C) and **engineering management** (audience D). Reached, but through the
   existing `operations.html`/`why.html` site pages rather than dedicated campaign content. **~10%.**
   Do not build a management content track for 1.0 — there are no case studies to put in it.

### 3.2 The wedge — one claim

> **Your business logic should not know how the message arrived.**
>
> In Benzene, the *same* handler is reachable over HTTP, SQS, SNS, Kafka, Service Bus and Event Hubs
> **at the same time**. Adding a transport is a wiring change, not a rewrite — and you can prove it
> with a unit test that never touches a cloud account.

**Why this wedge and not the others.**

It names a pain that is *daily, concrete and currently unsolved* inside a single cloud. A developer
who has written business logic inside an `SQSEvent` handler and then been asked to also expose it
over HTTP recognises this in one sentence. It does not require them to believe anything about
multi-cloud futures, to adopt an architectural ideology, or to buy a second product.
(`work/website-marketing-aims.md` §7b already made this call for the website; this campaign follows
it.)

**Two real files prove it, and both are unusually good demo material:**

- `examples/AwsMesh/Shared/MeshServiceWiring.cs` (`Configure`, lines 172–205) — **one handler array
  bound to five AWS event sources in ~20 lines**: API Gateway, direct Lambda invoke, SQS, SNS and
  EventBridge. The SQS/SNS/EventBridge blocks are three consecutive near-identical two-line stanzas;
  visually that is the money shot. A single generic `Observe<TContext>()` prelude
  (`UseW3CTraceContext` → `UseBenzeneEnrichment` → `UseBenzeneMetrics` → `UseLogResult`) wraps all
  five — an independent second proof that the cross-cutting concerns really are written once.
- `examples/Aws/Benzene.Examples.Aws.Tests/Integration/CreateOrderTest.cs` — **the same topic fired
  through SQS, SNS, API Gateway, BenzeneMessage-over-HTTP and direct invoke, in-process, in one test
  class**, built on `BenzeneTestHost.Create<StartUp>()` — the *same production `StartUp` you deploy*.

That second file is the strongest single artefact the project has, because it is the wedge and its
proof in the same screenshot: the claim isn't "trust us, it's decoupled", it's "here is a test
suite that only compiles if it is." `docs/cookbooks/testing-lambda-functions.md` already states the
headline for us — *"without deploying or running SAM local."*

**Honesty constraints on this wedge, to hold in every post:**
- Say **"five AWS event sources"**, not "five transports" unqualified — AwsMesh is one cloud, and a
  reader who discovers that after being told otherwise will not forgive it.
- AwsMesh is a **deploy-to-AWS example with Terraform**, not something a reader runs locally. Do not
  imply a local `run.sh` experience it does not have.
- The sibling meshes are **not** equivalent and must not be described as if they were:
  AzureFunctionsMesh delegates per-trigger ingress to each Function App project (real fan-out across
  Service Bus / Event Hub / Event Grid, but not one array in one file), GoogleCloudMesh wires two
  (HTTP + Pub/Sub), and K8sMesh wires one.
- `CreateOrderTest.cs` contains commented-out assertion blocks. Screenshot around them, or ask for a
  tidy-up first.
- In-process testing does **not** replace testing IAM, event-source-mapping config, or cold starts —
  and there is a separate LocalStack suite (`Benzene.Examples.Aws.Dev.Test`) that exists precisely
  because emulation still has a job. Say so in E2; it costs nothing and buys the reader's trust.

**Testability is the wedge's evidence, not a competing wedge.**

### 3.3 The other angles — what happens to each

| Angle | Verdict | Reasoning |
|---|---|---|
| **In-process testability without deploying/emulating** | **Promoted to the lead *search* hook** — it is the query people actually type ("test AWS Lambda handlers locally C#"), so it leads the evergreen posts, then hands off to the wedge | It is a consequence of transport decoupling, not a differentiator on its own (WebApplicationFactory, LocalStack, and Testcontainers all occupy adjacent ground). It gets people in the door; the wedge is what keeps them |
| **Hexagonal / ports-and-adapters purity** | **Secondary — architect-facing only** | It converts architects and it is genuinely what Benzene *is*. But led with, it sounds ideological, and "hexagonal architecture" posts attract people who want to argue about definitions rather than adopt a library |
| **Multi-cloud portability** | **Demoted to a closing bonus line. Do not lead with it, anywhere.** | Teams already on AWS do not move to Azure. `website-marketing-aims.md` §7b already demoted it; I agree and go further — see §6.4, it is actively *counterproductive* in vendor conversations. Keep it as "and it runs wherever you already are" |
| **Mesh / estate visibility** | **Held for Phase 3 (month 6+). Not in the launch. This is the right call and the internal reviews confirm it.** | There is a genuinely real product here — 20 `Benzene.Mesh.*` packages, a 5,012-line dependency-free UI, a published Mesh Explorer demo, and a Docker-Compose host in `deploy/Mesh`. But `work/mesh-drains-up-review.md` (2026-07-25) is blunt about the jobs it does: traffic *"partial, scattered"*, issues *"frame exists, watching the wrong signals — a system throwing errors all night says 'All clear'"*, resolution *"essentially unserved"*, with Phases 3–4 open and a STOP list on new surfaces. Marketing an observability product whose own review says it can miss an outage is the fastest way to lose the trust the rest of the campaign builds. It earns its own mini-launch once the front door and issue detail land |
| **Cloud Service spec / cross-language story** | **Drop as a campaign theme. Keep as one honest README line.** | Stronger than I first assumed — `docs/specification/` carries **real language-neutral JSON conformance fixtures** with a .NET runner (`test/Benzene.Conformance.Test`), and an external Go port is referenced (`daniellepelley/benzene-go`). But every spec doc is marked **`Status: DRAFT v0.1`**, `versioning.md` says "not yet implemented", the Go port is unverified from this repo and is already a named deferral behind on `mesh:issues`, and the .NET implementation is explicitly still "the single normative reference". Honest framing if it comes up: *"a draft language-neutral spec with shared conformance fixtures, plus an early Go port"* — **never** "Benzene is multi-language". A footnote for architects who dig, not a headline |

---

## 4. The surfaces — the 360° map

The point is that the same person meets Benzene from independent directions within a short window.
Ordered by when each turns on.

| Surface | What it's for | Phase | Cost |
|---|---|---|---|
| **Owned — the blog** | The spine. Every other surface points at a post. Compounds. | 0 onward | High (it *is* the work) |
| **Owned — site, README, NuGet, release notes** | The conversion surface. Must be perfect *before* traffic arrives, not after. | 0 | Low |
| **Search** | The compounding asset. Problem-first posts that rank keep earning after the spike decays. Rank for the *problem*, introduce the tool at the end. | 0 onward | Free, slow (3–6 months to rank) |
| **Aggregators & newsletters** | Highest leverage per hour available to us. A Morning Brew / dotNET Weekly pickup costs one polite email and reaches the exact audience. | 1 onward | Very low |
| **Community (r/dotnet, HN, lobste.rs, Discord)** | The spike, and the honest stress-test. Where objections surface first. | 1 | Low hours, high risk (§9) |
| **Syndication (dev.to)** | Second-chance distribution for evergreen posts. Canonical link back to `benzene.app`. | 1 onward | Very low |
| **Voice & video (podcasts, OSS webinars)** | Trust transfer. The single best format for the origin story and the "who's behind this" objection. Needs a story, not a feature list. | 2 | Medium, lumpy |
| **Events (user groups → conference CFPs)** | Cheap, real, underrated. A 30-minute user-group talk produces a recording, a slide deck and 3 conversations. CFPs need 3–6 months lead. | 2–3 | Medium |
| **Peer & influencer** | Low-volume, personal, no asks for promotion — offer something useful. | 2 | Low |
| **Vendor ecosystems (AWS, then Microsoft)** | Reach + institutional credibility. Requires evidence first. | 3 | Medium |

**Surfaces deliberately not bought:** see §9.

---

## 5. The content programme — the blog spine

Two kinds of post, and the difference matters:

- **Evergreen (E)** — problem-first, answers a query someone already types, mentions Benzene only in
  the last third. These keep earning for years. **Publish before the launch** so the launch lands on
  a blog with substance rather than a diary entry.
- **Launch (L)** — spike then decay. Concentrated in launch week.
- **Sustain (S)** — the drumbeat that keeps the project alive after the spike.

Cadence target: **one post every two weeks in Phase 0–1, one per month thereafter.** That is the
sustainable ceiling for one person who also maintains 155 packages.

### Phase 0 — evergreen, pre-launch (no promotion of Benzene itself)

| # | Title / premise | Audience | Question it answers | Distribution |
|---|---|---|---|---|
| **E1** | **"What happens to a failed message on every AWS and Azure transport — a reference table"** | Dev + SRE | "If my handler returns a failure on SQS/Kafka/Service Bus/Event Hubs, is the message retried or silently lost?" | r/dotnet, r/aws, lobste.rs, dev.to, HN (as a reference, not a launch) |
| | **This is the highest-confidence asset in the whole plan and should be written first.** It is ~80% already written in `docs/capability-matrix.md`'s per-transport breakdown, nothing equivalent exists on the public internet, it is pure utility, and it is a natural link magnet. It mentions Benzene almost incidentally — which is exactly why it works. | | | |
| **E2** | **"Test your AWS Lambda handlers without deploying — and without LocalStack"** | Dev | "How do I unit-test a Lambda handler that takes an `SQSEvent`?" | r/dotnet, r/aws, dev.to, Morning Brew |
| | The lead search hook. Grounded in `docs/testing-benzene.md` and `docs/cookbooks/testing-lambda-functions.md`, whose own title is already the headline: *"…End-to-End Without Deploying."* Show `CreateOrderTest.cs` — the same topic through five entry points, in-process. Honest about what it does *not* replace (IAM, event-source-mapping config, cold starts — and note that the LocalStack suite still exists for a reason). **Blocked until the `Benzene.Tools` doc bug (§1.2) is fixed.** | | | |
| **E3** | **"Your SNS-triggered Lambda can't also read SQS. That's an architecture problem, not an AWS one."** | Dev + architect | "Why do I keep writing the same logic twice for two event sources?" | r/dotnet, r/aws, dev.to |
| | **The wedge post.** Problem-first: name the pain for two-thirds of the piece, then show `MeshServiceWiring.Configure` — one handler array, five AWS event sources, ~20 lines. Say "AWS event sources", not "transports" (§3.2). | | | |
| **E4** | **"Hexagonal architecture in C#, without the ceremony"** | Dev + architect | "What does ports-and-adapters actually look like in a real cloud service?" | r/dotnet, r/csharp, dev.to |
| | High, persistent search volume, currently served by abstract diagram posts. Ours has runnable code. Sets up the architecture-fit argument for audience B. | | | |

### Phase 1 — launch week

| # | Title / premise | Audience | Question it answers | Distribution |
|---|---|---|---|---|
| **L1** | **"Benzene 1.0 — write your service once, run it behind any transport"** | All | "What is this and should I care?" | Everything, same week: HN (Show HN), r/dotnet, newsletters, dev.to, GitHub release notes, NuGet descriptions |
| **L2** | **"Why I built Benzene: 400 Lambdas and the lesson we learned"** | Dev + architect + management | **"Who is behind this and why should I trust it?"** — the single most important objection | HN, r/dotnet, r/aws; the pitch for every podcast in §6.3 |
| | Built directly on the maintainer's published 2023 Digiterre experience report. This is the trust post. It is also, by a distance, the most *interesting* thing the project has to say — a real production war story beats any feature list. **Do not skip it and do not soften it.** | | | |
| **L3** | **"What Benzene deliberately doesn't do"** | Architect + tech lead | "Where will this bite me in six months?" | HN, lobste.rs, r/dotnet |
| | Straight from `docs/capability-matrix.md`: no database abstraction, no cross-instance idempotency, no durable saga resume, no transport abstraction *by design*. Counter-intuitive, highly shareable, pre-empts the objections that would otherwise land as hostile comments on L1 — and publishing it in the same week as L1 is what makes the launch read as honest rather than promotional. | | | |

### Phase 2–3 — sustain

| # | Title / premise | Audience | Question it answers | When |
|---|---|---|---|---|
| **S1** | **"Benzene, MassTransit, Wolverine, Dapr and minimal APIs: an honest comparison"** | Architect + tech lead | "Why not just use the thing I already know?" | T0 + 1 month |
| | High-intent search, high risk. Must be scrupulously fair — where MassTransit or Dapr is genuinely the better answer, say so explicitly. Get it reviewed by someone who likes the alternatives before publishing. This post earns more trust than it costs. | | | |
| **S2** | **"Idempotency on at-least-once transports: what no framework can do for you"** | Architect + SRE | "How do I stop double-processing?" | T0 + 2 months |
| | The honest version — `docs/capability-matrix.md` already states that cross-instance dedup can't be solved inside Benzene. Strong architect credibility. | | | |
| **S3** | **"Sagas without a durable orchestrator — and when you actually need Step Functions"** | Architect | "Can I do multi-step distributed operations without Temporal?" | T0 + 3 months |
| **S4** | **"One handler, five AWS event sources: a walkthrough of the AwsMesh example"** | Dev | "Show me the whole thing working" | T0 + 4 months |
| **S5** | **"See your whole service estate, generated from your code"** | Architect + management | "What is my platform actually doing?" | T0 + 6 months — **the mesh mini-launch.** Double-gated: on UI polish (`website-marketing-aims.md` §5) **and** on `mesh-drains-up-review.md`'s Phase 3–4 closing. Needs screenshots that do not exist yet, and must inherit the site's own honest register — *"shipped and evolving"* (`website/generator/MarketingPages.cs`), never "an observability product" |

**13 posts over 8 months.** If the cadence slips, cut S3 and S4 first, then E4. Never cut E1, L2 or
L3 — those three carry the campaign.

**Rule for every post:** it links onward to one docs page and one runnable example, and no post ships
without a claims-check against a real file.

---

## 6. Outreach & partnerships

All outreach is **personal, low-volume, and easy to decline**. No mass mail. No astroturfing. The
maintainer sends; nothing here is sent on the project's behalf by anyone else.

### 6.1 Newsletters & aggregators — do this first, it is the cheapest reach we have

| Target | Ask | Notes |
|---|---|---|
| **The Morning Brew** (Chris Alcock, `blog.cwa.me.uk`) | Email the link to E1 and L1. He curates daily and links good .NET content without being asked. | **Needs verification:** the most recent issues surfaced in search are from 2024; confirm it is still publishing before investing. |
| **dotNET Weekly** (`dotnetweekly.com`) | Submit each evergreen post via the site's link-submission flow. | Submission appears to need an account. Site returned 403 to automated fetch — **verify the submission mechanism manually.** |
| **ASP.NET Core News** (`aspnetcore.news`) | Submit E2, E3, L1. | Weekly ASP.NET-focused roundup. |
| **.NET News** (`dotnetnews.co`) | Submit posts as published. | Daily curated .NET content. |
| **Reddit r/dotnet** | Post E1/E2/E3 as *content*, L1 as the launch. | **Needs verification:** read the current sidebar self-promotion rules before the first post — automated search could not retrieve them, and getting the project flagged as spam on the single most valuable community would be a permanent own-goal. Establish a posting history with the evergreen posts before ever posting a launch. |
| **Reddit r/aws, r/csharp** | E1, E2, L2 | r/aws is a genuinely good fit for E1 and the origin story. |
| **Hacker News** | Show HN for L1; E1 and L3 submitted on their own merits. | See the risk note in §9. Launch to r/dotnet *first*, HN second. |
| **lobste.rs** | E1, L3 | Small, high-quality, allergic to marketing. Only submit the honest/technical posts. |
| **dev.to** | Syndicate every evergreen post with a canonical link back to `benzene.app`. | Free second distribution. Own domain always publishes first. |

### 6.2 Communities

- **.NET Discord / C# Discord community servers** — participate genuinely; answer questions in the
  areas Benzene touches (serverless, messaging, testing). Do not drop links. Value accrues over
  months, not weeks.
- **Stack Overflow** — answer real questions about testing Lambda handlers and sharing code across
  Azure Functions/ASP.NET Core. Disclose affiliation every time. High-quality answers that happen to
  mention Benzene at the end age extremely well.

### 6.3 Podcasts — pitch the *story* (L2), never the feature list

| Target | Ask | Why them |
|---|---|---|
| **The Modern .NET Show** (Jamie Taylor, `dotnetcore.show`) | Guest pitch: "hundreds of Lambdas, and what we learned" | **Best first target.** UK-based, guest-driven format, actively takes community projects, most gettable. There is a public guest FAQ repo (`jamie-taylor-rjj/Podcast-FAQs`); **verify the current guest-submission route.** |
| **The Unhandled Exception Podcast** (Dan Clarke) | Guest pitch, same story | UK. Dan also runs **.NET Oxford** — one relationship, two surfaces. Approach once, mention both. |
| **.NET Rocks!** (Carl Franklin, Richard Campbell) | Guest pitch — **only after** a podcast appearance and a user-group talk exist | The biggest .NET podcast; will not book an unknown project with no traction. Approach in Phase 3 with evidence. |
| **Azure DevOps Podcast** (Jeff Palermo) | Guest pitch, Azure-angled version of the story | Architecture-leaning audience; the ports-and-adapters angle plays here. |

### 6.4 Video / webinar

- **JetBrains "OSS Power-Ups"** (`blog.jetbrains.com/dotnet`) — a real, long-running webinar series
  spotlighting open-source .NET projects (verified past episodes: Serilog, bUnit, QuestPDF, SpecFlow,
  MassTransit, Silk.NET). **This is close to an ideal fit.** The submission route is not publicly
  documented — **needs verification**; the practical path is a direct approach to the JetBrains .NET
  advocacy team. Phase 2, once there are posts and a demo to show.

### 6.5 User groups and conferences

Start local and cheap; conference CFPs need long lead times.

| Target | Ask | Lead time |
|---|---|---|
| **London .NET User Group** | Submit a 30-min talk via its **Sessionize Call for Speakers** (verified live) | Phase 2 |
| **dotnetsheff** | Submit via its **Sessionize CFS** (verified live), or email the organisers | Phase 2 |
| **.NET Oxford** | Approach Dan Clarke (see §6.3) | Phase 2 |
| **DDD conferences** (DDD East Midlands / North / South) | Submit; agendas are community-voted, which favours a genuinely interesting story over a known name | 3–4 months ahead |
| **NDC London** | CFP | 6+ months; Phase 3 only, with a recording to point at |
| **.NET Conf** | Call for content | **Needs verification for 2026** — the 2025 call opened around June with an August deadline for a November event. Check `dotnetconf.net` / the .NET Blog. Phase 3. |

**Talk title (one talk, reused everywhere):** *"Your handler shouldn't know it arrived by HTTP"* —
the L2 war story, live-coded into the multi-transport demo, ending on the mesh view.

### 6.6 People — approach with something useful, never with a request for promotion

- **Derek Comartin (CodeOpinion)** — the closest fit in the entire .NET ecosystem: messaging,
  event-driven architecture, loose coupling, all day. Ask: share E1 (the failure-semantics table) as
  something he might find genuinely useful. No promotion ask.
- **Steve Smith (Ardalis)** — clean/hexagonal architecture authority. Ask: a read of E4 or S1.
- **Jimmy Bogard** — messaging and distributed-systems credibility. Ask: a fairness review of S1 (the
  comparison post) *before* publication. Asking a potential competitor's peer to check you haven't
  misrepresented the alternatives is both the honest move and a genuine relationship-builder.
- **Khalid Abuhakmeh / the JetBrains .NET advocacy team** — the route to OSS Power-Ups (§6.4).
- **Nick Chapsas, Milan Jovanović** — very large audiences, and both monetise reach. **Do not
  approach in Phase 1–2** and do not pay for placement (see §9). Revisit only if organic traction
  makes Benzene interesting to them on its merits.

---

## 7. Microsoft / AWS — recommendation

### 7.1 The recommendation

**Yes — pursue AWS first, Microsoft second, and neither before the 1.0 tag plus roughly three months
of published evidence.** Approach them for *content placement and community programmes*, not for
partnership. Do not restructure the project or the messaging to make either happy.

### 7.2 Why AWS first

Benzene's origin story, its deepest package coverage, and its most compelling material are all AWS
Lambda. And there is a concrete, named venue: the **`.NET on AWS` blog**
(`aws.amazon.com/blogs/dotnet`), which exists specifically for .NET-on-AWS content and has a
**verified precedent for exactly this**: a post co-authored with Tomáš Herceg, founder of DotVVM, an
open-source .NET framework. That is the shape of the ask — not "promote my framework" but "here is a
story that makes Lambda look good to .NET developers."

**The AWS-facing story:** *"Consolidating hundreds of Lambdas into testable, maintainable services"*
— i.e. the L2 war story with the ending "and serverless was the right call all along; the granularity
was the problem." That is true, it is on-vision (`benzene-vision.md` §2.2), and it makes AWS look
good. **Not** the portability story.

**Programmes, with their real entry requirements:**

| Programme | Reality | Entry requirement | When |
|---|---|---|---|
| **`.NET on AWS` blog contribution** | Achievable. Verified precedent with an OSS framework founder. | A pitch to the blog's editors / a .NET-on-AWS developer advocate, with published content behind it. **Needs verification:** the exact contribution route is not publicly documented — the practical path is a named advocate, found via the AWS .NET community page. | T0 + 3 months |
| **AWS Community Builders** | Achievable, individual-level (the maintainer joins, not the project). Verified: cohort-based applications, requires an AWS Builder ID and **at least two pieces of high-quality public content created before the application window opens**. | The Phase 0–1 blog programme *produces this requirement as a by-product*. The 2026 cycle closed 21 January 2026, so the next window is likely late 2026 / January 2027 — **verify the exact date**. | Apply at the next window |
| **AWS Heroes** | Not realistic at this stage. Invitation-only, requires sustained years-long community impact. | — | Not now |
| **Formal partnership / joint press release** | Not available to a solo-maintainer project. Pretending otherwise wastes months. | — | Never, at this scale |

### 7.3 Why Microsoft second, and what is actually available

Microsoft has **no equivalent open "apply here" door for an OSS project**, which is why it sequences
second rather than first:

| Programme | Reality | Entry requirement | When |
|---|---|---|---|
| **Microsoft MVP (Developer Technologies)** | An *outcome* of this campaign, not an input. Verified: **you cannot self-nominate** — nomination comes from a current MVP or a Microsoft employee, and requires demonstrable community contribution over the preceding 12 months. | Do the campaign; the nomination follows or it doesn't. | Not an action item |
| **.NET Foundation project membership** | The highest-value Microsoft-adjacent credibility signal available, because it directly attacks Benzene's biggest objection ("will this exist in three years?"). Verified: a public **New Project Application**, reviewed by the Project Committee within a month, with tiers **Applicant → Seed → Member**; Seed means eligibility met but *activity* requirements not yet met. | **Needs verification** — the site returned 403 to automated fetch, so the precise Activity criteria are unconfirmed. My read is that a single-maintainer project with **zero external contributors** would land at *Seed*, not *Member*. That is still worth having, and the application is cheap. | Apply at T0 + 3 months, expecting Seed |
| **.NET Community Standup appearance** | Achievable but relationship-driven — the standups regularly feature community guests. | No open application; needs a contact on the .NET team, realistically reached via a .NET Foundation relationship or an MVP introduction. | Phase 3 |
| **`devblogs.microsoft.com/dotnet` mention** | Occasional community round-ups. Not directly solicitable. | — | Opportunistic |

**The Azure-facing story** (if a Microsoft venue opens): *"One handler across Service Bus, Event Hubs,
Queue Storage and HTTP triggers"* — the transport-mixing wedge, told entirely inside Azure. Benzene's
Azure trigger matrix is genuinely broad and is called out as a strength in `1.0-release-plan.md` §3.

### 7.4 The independence trade-off — stated openly

**Vendor association buys reach and institutional credibility; it costs message control, and it can
make the other vendor's community cooler on you.** Benzene's position is genuinely awkward here,
and I would rather name it than paper over it:

- **The portability claim cannot go in a vendor's blog.** No AWS or Microsoft venue will publish
  "and you can leave when you want to." That is fine — §3.3 already demotes portability to a closing
  bonus line. But it means vendor content is a *subset* of our story, never the whole of it.
- **The rule: each vendor gets the story that is true on their platform, and never a story that is
  untrue anywhere.** AWS gets consolidation-and-testability. Azure gets transport-mixing. Both are
  fully honest; neither is the complete picture; the complete picture lives on `benzene.app`.
- **The line I recommend holding:** if a vendor asks for the multi-cloud line to come off Benzene's
  *own* site or README as a condition, decline and lose the placement. Editing our own honest
  positioning to earn a blog post is precisely the trade that destroys the credibility the post was
  meant to buy.
- **Sequencing protects independence.** Approaching vendors *after* the launch, with our own audience
  already established, means we negotiate from a position where a "no" costs us a nice-to-have rather
  than the campaign.

**Net: worth doing, worth doing in this order, not worth reshaping the project for.**

---

## 8. Calendar and maintainer-hours

Assumption: **one maintainer, ~4 hours per week sustainably available for marketing**, with the
ability to clear one intensive week for launch. Everything below is designed against that ceiling.
Numbers are honest estimates including writing, editing and outreach, not just publishing.

### Phase 0 — Foundation (6 weeks, pre-tag) — **~45 hours (~7.5 h/week)**

The heaviest phase, because artefacts get built. This is front-loaded on purpose: it is the only
phase where you are not also responding to people.

| Work | Hours |
|---|---|
| Stand up a blog on `benzene.app` (generator work — route to the website owner, not marketing) | 6 |
| PNG/icon export from `Logo.cs` + `PackageIcon` wired centrally + social card template | 3 |
| Fix the `Benzene.Tools` doc bug; capture the missing screenshots and code images | 3 |
| Write and publish **E1** (the failure-semantics table) | 8 |
| Write and publish **E2** (testing without deploying) | 7 |
| Write and publish **E3** (the wedge post) | 7 |
| Write and publish **E4** (hexagonal in C#) | 6 |
| Pre-write launch-week assets: L1 draft, release notes, newsletter emails, the r/dotnet and HN posts | 5 |

*Zero promotion of Benzene-the-project in this phase.* The evergreen posts go out on their own merits
and start ageing into search. **If hours are short, cut E4 and ship in 5 weeks with three posts.**

### Phase 1 — Launch (2 weeks, T0) — **~22 hours, concentrated**

Requires a genuinely cleared week. Everything lands inside 7 days so the surfaces compound.

| Work | Hours |
|---|---|
| Cut the release: bump `version.txt`, tag, publish, GitHub release, drop prerelease badges | 4 |
| Publish **L1**, **L2**, **L3** across the same week | 8 |
| Submit to newsletters, r/dotnet, HN, lobste.rs, dev.to syndication | 3 |
| **Respond to everything, fast** — this is the phase that converts, and the one people underestimate | 7 |

### Phase 2 — Sustain (months 2–5) — **~64 hours (~4 h/week)**

| Work | Hours |
|---|---|
| **S1**, **S2**, **S3** (one post per month) | 24 |
| Podcast outreach + one recorded appearance | 8 |
| User-group CFS submissions + one talk (write once, deliver twice) | 16 |
| Community presence: Discord, Stack Overflow, issue responsiveness | 12 |
| Influencer outreach (§6.6) — 4 personal emails, spaced | 4 |

### Phase 3 — Second wave (months 6–8) — **~34 hours (~3 h/week)**

| Work | Hours |
|---|---|
| **S4**, **S5** (S5 = the mesh mini-launch, gated on UI polish) | 16 |
| Vendor approaches: `.NET on AWS` pitch, .NET Foundation application, AWS Community Builders | 8 |
| Conference CFP submissions (NDC London, .NET Conf, DDD) | 6 |
| Second podcast / OSS Power-Ups | 4 |

### Total: **~165 hours over 8 months (~4.8 h/week average)**

**This is at the edge of what one person can sustain alongside maintaining the library, and I have
already cut to fit.** What was cut, and why, is in §9. If real availability is closer to 2 h/week,
the honest plan is: **do Phase 0 (E1, E2, E3 only), do Phase 1 in full, then drop Phase 2 to one
post per two months and skip user-group talks.** A campaign that stops after launch is worse than a
smaller campaign that keeps going — the drumbeat is what turns a spike into adoption.

---

## 9. Measurement, risks, and what we are NOT doing

### 9.1 Indicators — and which are vanity

| Indicator | Honest? | Notes |
|---|---|---|
| GitHub stars | **Vanity.** Track it, never optimise for it. | Stars correlate with HN visibility, not usage. |
| HN points / Reddit upvotes | **Vanity**, but a useful same-day signal of whether the framing landed. | |
| NuGet downloads of `Benzene.Core` | **Misleading** — inflated by transitive pulls and CI. | |
| **NuGet downloads of a transport package** (`Benzene.Aws.Lambda.Sqs`, `Benzene.AspNet.Core`) **30+ days after the spike** | **Honest.** | The shape matters more than the number: a spike that decays to zero is attention; a flat line that persists is adoption. |
| **Issues/discussions opened by strangers** | **Honest, and the best early signal.** | Someone only files an issue after trying it. |
| Docs traffic from *organic search* (not referral) | **Honest.** | The measure of whether the evergreen strategy is working. Expect nothing for 3 months. |
| Newsletter/aggregator pickups | **Honest, binary.** | |
| **Someone unaffiliated using Benzene in something real** | **The one that matters.** | Target: 3 by T0 + 6 months. |

**Review points:** T0 + 2 weeks (did the launch land?), T0 + 3 months (is search working? are
strangers showing up?), T0 + 6 months (the real-usage test).

**"This isn't working" looks like:** at T0 + 3 months, fewer than 5 stranger-opened issues, no
organic search traffic growth, and no newsletter pickups. **Kill rule:** any channel that produces
zero qualified inbound after two honest attempts gets dropped, not nursed. If the *wedge* produces no
recognition after E3 and L1, the problem is the wedge, not the channel — revisit §3 rather than
posting more.

### 9.2 Risks

1. **Overclaim risk — and it is not hypothetical.** `1.0-release-plan.md` T1 found docs overselling
   code across many packages; the sweep fixed 111 package docs. Yet grounding *this plan* still
   surfaced a live one: `Benzene.Tools` is documented as an installable package and has no source
   (§1.2). If a single afternoon of fact-checking finds one, a motivated HN commenter will too.
   **Every post gets a claims-check against a real file before publishing**, and the claims-check
   list ships with the draft. One discovered overclaim and the whole project reads as hype.
2. **Launching before the tag.** If any promotion runs while packages are `-alpha` and
   `version.txt` is `0.0.2`, the launch is spent. **Hard gate: no Phase 1 activity before the tag.**
3. **The bus factor, and the AI-authored commit history.** `1.0-readiness-checklist.md` names the
   single-maintainer risk; and the public git history shows a large share of commits authored by an
   AI agent. Someone on HN or r/dotnet **will** notice and raise it. The recommended posture:
   **do not hide it, do not lead with it, and have a straight answer ready** — the maintainer directs
   the work and owns every design decision, the code is tested and reviewed, and the honest
   engineering culture (the capability matrix, the doc-truth sweep) is the evidence. Attempting to
   obscure it would be the single most damaging thing this campaign could do. Consider addressing it
   pre-emptively and briefly in L2, on our terms, rather than defensively in a comment thread.
4. **Hacker News downside.** A Show HN can attract a hostile "not another .NET framework" top
   comment that defines the thread. Mitigation: launch to r/dotnet first, submit to HN second, and
   publish L3 ("what it deliberately doesn't do") *before or alongside* L1 so the obvious objections
   are already answered in our own words.
5. **155 packages is itself an adoption objection.** "Which of these do I install?" is a real
   barrier and no blog post fixes it. `docs/reference/packages.md` exists; whether that is enough is
   a **DX question — route to the dx-champion**, not something marketing should paper over with
   copy. Related: three `src/` directories are stale build-output leftovers with no sources — trivial
   to remove, and the kind of thing a browsing evaluator notices.
6. **The name.** "Benzene" competes with chemistry for search and carries a mild carcinogen
   association. Not fixable and not worth fixing: the mark in `Logo.cs` turns it into an asset — a
   benzene ring *is* a hexagon, and hexagonal architecture *is* the pitch. Lean on the visual pun;
   always search-target "Benzene .NET", never "Benzene" alone.
7. **Comparison-post backfire (S1).** Misrepresenting MassTransit/Dapr/Wolverine would be both wrong
   and self-destructive. Mitigation: peer review before publishing (§6.6), and state plainly where
   the alternative wins.

### 9.3 What we are deliberately NOT doing

- **No YouTube channel.** Video is the highest-cost-per-artefact format and a channel needs cadence
  we cannot sustain. One conference-talk recording, produced as a by-product of a talk we were giving
  anyway, is the entire video strategy.
- **No paid newsletter sponsorship or paid influencer placement.** No budget assumed, and undisclosed
  paid placement is off-limits regardless.
- **No conference booths, no sponsorship.**
- **No daily social-media presence.** Posting each blog post once to the relevant places is the whole
  social plan.
- **No project Discord or Slack.** A dead community server signals a dead project. GitHub Discussions
  is enough until there is demand it can't absorb.
- **No case studies or testimonials** — there are no users yet, and inventing them is out of the
  question.
- **No management/procurement content track for 1.0.** The `why.html` site page covers audience D;
  a dedicated track needs evidence we don't have.
- **No Medium, no Dev.to-first publishing.** Own domain publishes first, always; dev.to is
  syndication with a canonical link.
- **No Google Cloud or Cloudflare messaging.** Both are explicitly out of 1.0 scope
  (`1.0-release-plan.md` §1) and marketing them would contradict the release's own honesty.
- **No mesh at launch, no cross-language spec as a theme.** See §3.3.
- **No astroturfing, no sockpuppets, no vote manipulation, no mass unsolicited outreach.** Ever.

---

## 10. Immediate next actions

Ordered. The first three are prerequisites for everything else.

1. **Set the 1.0 tag date** and work the calendar backwards from it. Phase 0 is six weeks; nothing in
   Phase 1 happens a day before the tag. *(Maintainer decision — blocks the whole plan.)*
2. **Stand up a blog on `benzene.app`.** The campaign spine currently has no home:
   `website/generator/` has no blog concept. Scope: a post list, a post page reusing the existing
   `Layout.cs` shell, markdown-sourced from a `blog/` directory, an RSS feed (newsletters consume
   RSS). *(Route to the website owner; ~6 hours.)*
3. **Export a raster logo from `Logo.cs`** and wire `PackageIcon` centrally in
   `src/Directory.Build.props` — `1.0-release-plan.md` Tier 0.7 already flags it as the one missing
   piece of NuGet polish. Same asset becomes the social card and the talk title slide. *(~3 hours.)*
4. **Fix the `Benzene.Tools` doc bug** in `docs/testing-benzene.md` (line 49) and
   `docs/cookbooks/testing-lambda-functions.md` — the correct package is
   `Benzene.Aws.Lambda.Core.TestHelpers`. **Hard prerequisite for E2**, and a live overclaim in the
   docs a launch will drive traffic to. *(Route to core/DX owner; ~30 minutes.)*
5. **Write E1 — the per-transport failure-semantics reference table.** Highest-confidence asset in
   the plan, ~80% already written in `docs/capability-matrix.md`, and it is the post that gets picked
   up on its own merits with no reputation behind it. *(~8 hours.)*
6. **Capture the visual assets that do not exist.** There is not a single screenshot in the repo.
   Needed before any post or talk: the Mesh Explorer demo, the Spec UI demo, and
   `MeshServiceWiring.Configure`/`CreateOrderTest.cs` as code images. *(~2 hours.)*
7. **Verify the five "needs-verification" items** before relying on them: which domain is actually
   serving the site, r/dotnet's current self-promotion rules (read the sidebar), The Morning Brew's
   2026 activity, dotNET Weekly's submission mechanism, and the .NET Foundation's precise Activity
   requirements. *(~1 hour total.)*
8. **Draft the launch-week runbook** — the exact order of the tag, the three posts, the newsletter
   emails and the community submissions, written down before launch week rather than improvised
   during it. *(~2 hours.)*
9. **Send one relationship email now, with no ask:** Derek Comartin (CodeOpinion), sharing E1 once
   published. The best time to start a relationship is months before you need it.

---

## 11. Related documents

- [`work/website-marketing-aims.md`](website-marketing-aims.md) — messaging pillars; §7b's
  repositioning is the direct basis for §3.2's wedge
- [`work/website-audience-plan.md`](website-audience-plan.md) — the four audiences behind §3.1
- [`work/benzene-vision.md`](benzene-vision.md) — the philosophy every claim must stay honest to
- [`work/1.0-release-plan.md`](1.0-release-plan.md) — the authoritative launch state behind §1.2
- [`work/enterprise-adoption-gap-analysis.md`](enterprise-adoption-gap-analysis.md) — the objections
  a tech lead will raise
- [`docs/capability-matrix.md`](../docs/capability-matrix.md) — the honest boundaries; source for E1,
  L3 and S2
- [`docs/testing-benzene.md`](../docs/testing-benzene.md) — source for E2's central claim
- [`work/service-mesh-roadmap-1.0.md`](service-mesh-roadmap-1.0.md),
  [`work/mesh-ui-product-vision.md`](mesh-ui-product-vision.md) — the Phase 3 mesh mini-launch
</content>
