# Benzene 1.0 — Marketing Campaign Plan

**Status:** DRAFT for maintainer review — **revision 2**, strategic shape, not finished copy
**Last Updated:** 2026-07-25 (rev 2 — production-provenance integration)
**Purpose:** A coordinated, phased campaign to take Benzene from ~zero awareness to a name a .NET
developer building cloud services has encountered from several independent directions. Blogs are
the spine; this document picks the wedge, designs the blog programme, names the channels and the
people to approach, gives a recommendation on Microsoft/AWS involvement, and states honestly what
it costs a solo maintainer in hours.

**Companions (build on, don't re-litigate):** [`work/website-marketing-aims.md`](website-marketing-aims.md)
(messaging pillars), [`work/website-audience-plan.md`](website-audience-plan.md) (audiences),
[`work/benzene-vision.md`](benzene-vision.md) (the philosophy copy must stay honest to),
[`docs/capability-matrix.md`](../docs/capability-matrix.md) (the honest boundaries),
[`work/1.0-release-plan.md`](1.0-release-plan.md) (the authoritative launch state),
**[`work/benzene-production-provenance.md`](benzene-production-provenance.md) — the source of truth
for every provenance claim in this plan. Nothing here overrides it; where they differ, it wins.**

---

## Revision note — 2026-07-25 (rev 2): the production-provenance integration

`work/benzene-production-provenance.md` landed after rev 1 and changes the trust strategy
materially. A forerunner library — materially the same design, significantly narrower scope — runs
in production on AWS in a flagship accountancy platform, spread team-to-team on its own merits, and
was then standardised on for new systems. **This is the most valuable marketing asset the project
has**, and rev 1 was written without it.

What changed in this revision:

| § | Change |
|---|---|
| **§3.0 (new)** | **The trust line** — the exact canonical wording, named and anonymised, written once. Every other artefact quotes it verbatim. |
| **§3.2** | **The wedge is unchanged.** §3.2.1 explains why, and what the provenance *does* change: it becomes third-party evidence under the wedge's DX claim, rather than replacing it. |
| **§3.3 (new)** | **Trust promoted to a second pillar** alongside the wedge, with the permission-path matrix: what is blocked on naming permission, what proceeds anonymised today. |
| **§5** | L1, L2, L3 rewritten around the lineage; **L2 is now the lineage post**. New **S6** ("how a library spreads without a mandate") displaces S3. |
| **§6** | New **§6.0 — the permission and quote ask**, sequenced first because it has the longest lead time in the whole plan. Podcast and HN/Reddit framing updated. |
| **§7** | The AWS story is materially stronger — the precursor runs on AWS in a real accountancy platform. Pitch upgraded; timing held. |
| **§8** | Re-costed: **+7 hours**, paid for by moving **E4** out of Phase 0. Net **+1 hour**. |
| **§9** | New risks: provenance overclaim drift, the IP/lineage gate, and the "so it's not actually been used" reading. |
| **§10** | Actions reordered — the permission ask is now **action 1**. |

**The one rule this revision exists to enforce:** the defensible claim is *"the design is proven in
production, in a narrower scope"*. The indefensible one is *"Benzene is battle-tested"*. Everything
below is downstream of that distinction.

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
- **And — new since rev 1 — the project has a production lineage.** Per
  `work/benzene-production-provenance.md`: a narrower forerunner of Benzene's design runs in
  production on AWS in a flagship accountancy platform, spread from one team to others voluntarily,
  and was then standardised on for new systems. It is not marketable as-is — §4 of that document
  lists unresolved permission and IP gates — but it changes what the campaign is capable of
  claiming, and it is the reason for this revision.

  **The connection to the Digiterre report is an open question the maintainer must answer**, and it
  is not cosmetic: the vision doc describes the same shape of engagement ("a large B2B software
  system, built entirely on AWS Lambda, serving thousands of businesses across the UK"). If the
  precursor was built as a consultant delivering into a client, the IP/clean-lineage gate
  (provenance §4.2) is *more* important, not less, and the answer determines whether the maintainer
  is even the right person to ask for permission. Resolve before any provenance copy ships.
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
question, not only the feature question. **Rev 1 had no good answer to that question beyond "the
engineering is honest"; rev 2 does (§3.0), and that is the single biggest change to this plan.**

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

### 3.0 The trust line — written once here, quoted verbatim everywhere else

This is the campaign's single most sensitive piece of copy. It is written **once**, in this section.
Every other artefact — README, site hero, L1, L2, L3, newsletter emails, podcast prep, vendor
pitches, conference abstracts — **quotes one of these forms verbatim rather than paraphrasing.**
Paraphrase is how "the design is proven in a narrower scope" becomes "it's battle-tested" over six
months of retelling, and nobody notices until a commenter does.

#### The canonical paragraph — anonymised form (usable the moment gate 2 clears; **this is the default**)

> **Benzene 1.0 is a new release, but it is not a new idea.** A narrower forerunner of this design —
> the same ports, message handlers and middleware pipeline — has been running in production on AWS
> in a flagship accountancy platform at a UK software group. It was built by one team; other teams
> adopted it because it was easier to build with than the alternatives, and it was later
> standardised on for new systems. Benzene is the next generation of that design, broadened well
> beyond AWS Lambda — to Azure Functions, Kubernetes, Google Cloud and anything running Docker.
> **That broader surface is new, and has not been proven in production.**

#### The canonical paragraph — named form (only after gates 1 **and** 2 clear; see §3.3)

Identical, with the second sentence replaced by:

> …has been running in production on AWS in **IRIS Elements, a flagship accountancy platform at
> IRIS Software Group**.

Nothing else changes. The named form is the anonymised form with a name in it — deliberately, so
that swapping between them is a find-and-replace and not a rewrite, and so that no artefact is
structurally dependent on permission arriving.

#### Derived short forms

| Where | Form |
|---|---|
| **README / site hero sub-line** (≤2 sentences) | "The design has production history: a narrower forerunner runs on AWS in a flagship accountancy platform, where teams adopted it because it was easier to build with. Benzene generalises it beyond AWS — and that part is new." |
| **HN / Reddit body, newsletter email** (1 sentence) | "Benzene itself is 1.0 and unproven; the design it generalises has been in production on AWS in a flagship accountancy platform for years, which is a claim about the design and not about this code." |
| **Spoken form** (podcast, talk, user group) | "I'm not going to tell you Benzene is battle-tested — it's a 1.0. What I can tell you is where the design came from and that it's been carrying a real product on AWS in a narrower form, and that other teams picked it up without being told to." |

#### Rules of use — non-negotiable

1. **The limit clause travels with the claim.** "That broader surface is new and has not been proven
   in production" is not a caveat to be dropped for space. Volunteering the limit is precisely what
   makes the rest believable — a reader who is told the boundary trusts the claim inside it.
2. **If a format cannot fit the limit clause, do not make the claim in that format.** A 60-character
   sub-head, a tweet, a NuGet package summary: these get the wedge, not the provenance. This rule
   resolves every "but there isn't room" argument in advance.
3. **Banned phrasings, permanently:** "battle-tested", "production-proven" *(unqualified)*,
   "enterprise-grade", "trusted by", "used by IRIS", "IRIS runs Benzene", "powers a flagship
   accountancy platform" *(present tense, of Benzene)*. Each is a claim about **Benzene**; the true
   claim is about **the design**.
4. **Never imply endorsement, sponsorship or a commercial relationship.** A company having used a
   precursor is not a company endorsing this project. If a cleared quote is obtained (§6.0), it is
   attributable to a named individual about the *precursor*, not a corporate endorsement of Benzene.
5. **Every use of the trust line carries a link** to the fuller explanation (L2 in Phase 1;
   `work/benzene-production-provenance.md` is internal and does not get published as-is).

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

### 3.2.1 Does the production provenance change the wedge? No — and here is why

New information arrived, so the question deserves a real answer rather than a reflex in either
direction. **The wedge stays exactly as written in §3.2.** Three reasons, in order of weight:

1. **A wedge has to be a problem the reader already has. Provenance isn't one.** Nobody types "what
   framework does an accountancy platform use" into a search bar or scans an r/dotnet title for it.
   "Your business logic should not know how the message arrived" is recognised in one sentence by
   someone who has lived it. Provenance answers the reader's *second* question, not their first —
   it is a closer, not an opener, and closers make bad wedges.
2. **A wedge that can be blocked by a third party is not a wedge.** The provenance is gated on
   permission and an IP check the maintainer does not fully control (§3.3). Building the campaign's
   central claim on an asset that might be withdrawn — or delayed by three months of corporate
   comms — would mean the whole plan waits. The wedge must be publishable today. This point alone
   is decisive.
3. **Leading with provenance invites the exact overclaim trap §3.0 exists to prevent.** A title or
   hero line built on "runs in production at a major software group" will be read as a claim about
   *Benzene*, then corrected in the comments by a reader who checks NuGet and finds a 1.0 published
   last week. That is the single worst outcome available to this campaign, and it is entirely
   self-inflicted.

**What the provenance genuinely changes — two things, both significant:**

- **It is third-party evidence under the wedge's DX claim, and the campaign should use it there.**
  Rev 1 could only assert that Benzene is easier to build with; the maintainer saying so is worth
  nothing. "It was built by one team, and other teams adopted it voluntarily because it was easier
  than the alternatives" is the same claim made by people with no stake in it. Developer-led
  adoption inside a company is the closest thing to a controlled experiment this project will ever
  have. The evidence line, for use under the wedge wherever there is room:

  > *This isn't a hypothesis about developer experience. In the forerunner, adoption spread team to
  > team without a mandate — people picked it up because it was less work than the alternative.*

- **It promotes trust from a risk to be mitigated into a pillar to be led on — second, after the
  wedge.** See §3.3.

### 3.3 The second pillar: trust — and the permission paths

Rev 1 treated "who's behind this, will it exist in three years" as a **risk** handled defensively in
one post (L2). That was the right call with the assets rev 1 had. It is the wrong call now.

**Pillar structure for the campaign, in order:**

| | Pillar | Claim | Answers | Lead surface |
|---|---|---|---|---|
| 1 | **The wedge** | Your business logic should not know how the message arrived | "Why would I use this?" | Evergreen posts, search, r/dotnet, L1 |
| 2 | **The lineage** | The design is proven in production, in a narrower scope — and the broader surface is new | "Why would I trust this?" | README/site trust block, L2, tech-lead material, vendor pitches, podcasts |
| 3 | **The boundaries** | Here is exactly what it deliberately doesn't do | "Where will this bite me?" | L3, `docs/capability-matrix.md` |

Pillars 2 and 3 do the same job from opposite directions and reinforce each other: a project that
volunteers both its production history *and* its limits reads as neither hype nor vapour. Publishing
them in the same week is the point.

#### 3.3.1 Two gates, two different blast radii — do not conflate them

This distinction is not in the provenance doc explicitly and it matters operationally:

| Gate | Blocks | Consequence if unresolved |
|---|---|---|
| **Gate 2 — IP / clean-lineage** (provenance §4.2) | **All provenance copy, named *and* anonymised.** The anonymised form still advertises the lineage; anonymity is not a defence against an IP question. | **The whole trust pillar is off the table.** Campaign reverts to rev 1's posture. Resolve this first — it is cheap (one conversation, possibly one contract re-read) and it gates everything. |
| **Gate 1 — permission to name** (provenance §4.1) | Only the *named* form and the named-only artefacts below. | Campaign proceeds in full on the anonymised form. Nothing on the critical path stops. |

Gates 3 (precision on the architect's decision) and 4 (the credibility specifics — years in
production, number of teams/services, rough scale, start year) block only the **sentences that use
them**. Until gate 3 clears, write "was later standardised on for new systems" and not a
quantified or quoted version. Until gate 4 clears, do not put a number anywhere.

#### 3.3.2 What needs the named version, and what does not

**Fine anonymised — proceeds today (once gate 2 clears), no permission needed:**

| Artefact | Note |
|---|---|
| README trust block, site hero sub-line | §3.0 short form |
| **L1** (launch post), **L2** (the lineage post), **L3** (the boundaries post) | L2 loses force without the name but stands up — the *organic adoption* story is the interesting part, and it is interesting without a company name |
| HN / r/dotnet / lobste.rs framing | Body text, never the title (§6.1) |
| Newsletter pitch emails | |
| All evergreen posts (E1–E4) | They barely touch it; that is by design |
| Podcast appearances | **With a scripted answer prepared.** Live speech is where anonymised claims get accidentally named or inflated — brief this before recording, do not improvise it |
| Conference / user-group abstracts | |
| .NET Foundation application | Helpful, not required |

**Needs the named version to work at all — do not build the calendar around these:**

| Artefact | Why the name is load-bearing |
|---|---|
| **A cleared quote from the chief architect** | An anonymous quote from an anonymous architect at an anonymous company is worth approximately zero. Named or not at all |
| **A case study** | Same — and needs the company's own sign-off on the content, not just the name |
| **The `.NET on AWS` blog pitch** (§7.2) | A vendor's editorial process will want a verifiable, named production story. An anonymised one is likely to be cut to nothing. **Note: AWS will need the customer's sign-off too — this artefact is double-gated and should be planned as a stretch, not a milestone** |
| **Any "trusted by" style logo or badge** | Not doing this regardless (§9.3), listed for completeness |

**The operational rule:** every artefact is drafted against the anonymised form. If permission
lands, a find-and-replace upgrades them all in an afternoon (§8, contingency hours). **No artefact
on the critical path is written in a way that assumes permission has been granted.**

### 3.4 The other angles — what happens to each

| Angle | Verdict | Reasoning |
|---|---|---|
| **In-process testability without deploying/emulating** | **Promoted to the lead *search* hook** — it is the query people actually type ("test AWS Lambda handlers locally C#"), so it leads the evergreen posts, then hands off to the wedge | It is a consequence of transport decoupling, not a differentiator on its own (WebApplicationFactory, LocalStack, and Testcontainers all occupy adjacent ground). It gets people in the door; the wedge is what keeps them |
| **Hexagonal / ports-and-adapters purity** | **Secondary — architect-facing only** | It converts architects and it is genuinely what Benzene *is*. But led with, it sounds ideological, and "hexagonal architecture" posts attract people who want to argue about definitions rather than adopt a library |
| **Multi-cloud portability** | **Demoted to a closing bonus line. Do not lead with it, anywhere.** | Teams already on AWS do not move to Azure. `website-marketing-aims.md` §7b already demoted it; I agree and go further — see §7.4, it is actively *counterproductive* in vendor conversations. **Rev 2 sharpens this: the provenance is an AWS story. Leading with portability now actively undercuts our strongest trust asset** Keep it as "and it runs wherever you already are" |
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

**The trust line (§3.0) runs across all of them.** That is what makes a 360° campaign compound
rather than just repeat: a reader who meets the wedge on r/dotnet, then hears the lineage on a
podcast, then finds the same limits stated in the README, is being told a consistent story by
someone who volunteers their own boundaries. Inconsistency across surfaces is the failure mode —
hence "quote it verbatim, never paraphrase".

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
| | **Rev 2 change:** carries a short *"where this design comes from"* section — three sentences, the §3.0 canonical paragraph verbatim, linking to L2. It goes below the wedge and the code, **not** in the opening. The post's job is still to explain what Benzene is; provenance is the reason to keep reading, not the headline. | | | |
| **L2** | **"The framework is new. The design has been in production for years."** *(alt: "Why I built Benzene: hundreds of Lambdas, and what happened next")* | Dev + architect + tech lead | **"Who is behind this and why should I trust it?"** — the single most important objection | HN, r/dotnet, r/aws; the pitch for every podcast in §6.3 |
| | **Rewritten in rev 2 — this is now the lineage post, and it is the most important post in the plan.** Structure: (1) the war story from the maintainer's published 2023 Digiterre experience report — the origin problem, told as it already exists in public; (2) what got built in response, and that it went into a flagship accountancy platform running on AWS; (3) **the organic-adoption section — one team built it, other teams adopted it because it was easier, and it was standardised on later.** This is the section a tech lead will remember; (4) **the limits, in our own words and unprompted** — Benzene is a 1.0, the design is what has the history, the broadened multi-cloud surface is new and unproven. Ends on §3.0's canonical paragraph. Anonymised by default; upgraded in place if permission lands. **Do not skip it and do not soften it — but equally, do not publish it until gate 2 (IP/lineage) is closed.** | | | |
| **L3** | **"What Benzene deliberately doesn't do"** | Architect + tech lead | "Where will this bite me in six months?" | HN, lobste.rs, r/dotnet |
| | Straight from `docs/capability-matrix.md`: no database abstraction, no cross-instance idempotency, no durable saga resume, no transport abstraction *by design*. Counter-intuitive, highly shareable, pre-empts the objections that would otherwise land as hostile comments on L1 — and publishing it in the same week as L1 is what makes the launch read as honest rather than promotional. **Rev 2 adds one section: "what is and isn't proven"** — the design has production history in a narrower, AWS-Lambda-centric scope; the Azure/Kubernetes/GCP surface is new; the mesh is early. Putting the limit of our best claim inside our "here's what we can't do" post is the most credible place it could possibly sit. | | | |

### Phase 2–3 — sustain

| # | Title / premise | Audience | Question it answers | When |
|---|---|---|---|---|
| **S1** | **"Benzene, MassTransit, Wolverine, Dapr and minimal APIs: an honest comparison"** | Architect + tech lead | "Why not just use the thing I already know?" | T0 + 1 month |
| | High-intent search, high risk. Must be scrupulously fair — where MassTransit or Dapr is genuinely the better answer, say so explicitly. Get it reviewed by someone who likes the alternatives before publishing. This post earns more trust than it costs. | | | |
| **S2** | **"Idempotency on at-least-once transports: what no framework can do for you"** | Architect + SRE | "How do I stop double-processing?" | T0 + 2 months |
| | The honest version — `docs/capability-matrix.md` already states that cross-instance dedup can't be solved inside Benzene. Strong architect credibility. | | | |
| **S6** | **"How a library spreads inside a company without anyone mandating it"** | Tech lead + engineering manager + dev | "What actually makes a team adopt an internal framework?" | T0 + 3 months — **new in rev 2** |
| | The organic-adoption story told as a *general* piece about internal developer platforms and pull-vs-push adoption, with the forerunner as the worked example. This is the highest-leverage new artefact the provenance unlocks: it is genuinely interesting to people who have never heard of Benzene, it is the one post that speaks natively to audience B/D without needing a case study, and it makes its point about Benzene almost incidentally. Works fully anonymised. Distribution: r/dotnet, r/ExperiencedDevs, HN, dev.to, and it is the best single artefact to send to Derek Comartin and to newsletter curators who have already run a Benzene link. **Takes S3's slot** (§8). |  |  |  |
| **S3** | **"Sagas without a durable orchestrator — and when you actually need Step Functions"** | Architect | "Can I do multi-step distributed operations without Temporal?" | **Demoted in rev 2 to opportunistic** — write it only if the cadence holds |
| **S4** | **"One handler, five AWS event sources: a walkthrough of the AwsMesh example"** | Dev | "Show me the whole thing working" | T0 + 4 months |
| **S5** | **"See your whole service estate, generated from your code"** | Architect + management | "What is my platform actually doing?" | T0 + 6 months — **the mesh mini-launch.** Double-gated: on UI polish (`website-marketing-aims.md` §5) **and** on `mesh-drains-up-review.md`'s Phase 3–4 closing. Needs screenshots that do not exist yet, and must inherit the site's own honest register — *"shipped and evolving"* (`website/generator/MarketingPages.cs`), never "an observability product" |

**13 posts over 8 months** (E4 moves out of Phase 0 to pay for the rev-2 additions — see §8). If the
cadence slips further, cut S3 and S4 first. **Never cut E1, L2, L3 or S6** — those four carry the
campaign, and rev 2 makes L2 the load-bearing one.

**Rule for every post:** it links onward to one docs page and one runnable example, and no post ships
without a claims-check against a real file. **Rev 2 addition: any post touching the provenance
claims-checks against `work/benzene-production-provenance.md` §2's table, quotes §3.0 verbatim, and
does not ship while gate 2 is open.**

---

## 6. Outreach & partnerships

All outreach is **personal, low-volume, and easy to decline**. No mass mail. No astroturfing. The
maintainer sends; nothing here is sent on the project's behalf by anyone else.

### 6.0 The permission ask and the architect's quote — **do this first, it has the longest lead time**

**A cleared, named quote from the chief architect on why they standardised on the precursor is worth
more than any post in §5.** It is also the only item in this plan whose timing is controlled by
someone else, which is exactly why it goes first: three months of corporate sign-off costs nothing
if it runs in parallel with Phase 0, and delays the launch if it starts in Phase 1.

**Sequencing — week by week:**

| When | Step |
|---|---|
| **Phase 0, week 1 — before anything else** | **Close gate 2 (IP / clean lineage) yourself.** Re-read whatever employment or consultancy contract governed the precursor; confirm Benzene is an independent implementation of the *design*, not derived code. This is a private, free, same-day action and it gates every provenance artefact in the plan. If it does not close cleanly, take legal advice before writing a word of L2 |
| **Phase 0, week 1** | **Answer the Digiterre question** (§1.1): who employed you to build the precursor, and does the published 2023 report already describe this engagement? If a consultancy delivered it into a client, *both* organisations may have a say, and you need to know which door to knock on |
| **Phase 0, week 2** | **Send one email.** To the chief architect directly — warm, personal, short. Not to corporate comms first; a friendly internal advocate routes it far better than a cold marketing enquiry |
| **Phase 0, weeks 3–8** | **Wait, and build everything anonymised in the meantime.** One polite follow-up at ~3 weeks. Then stop. Do not chase a third time — a corporate "no" or silence must cost this campaign nothing |
| **T0 (launch)** | Launch on whichever form is cleared. If permission arrives later, **upgrade the artefacts in place** and treat it as a sustain-phase beat, not a re-launch |

**Who asks:** the maintainer, personally, in his own name. Nobody asks on the project's behalf.

**What exactly to ask for — three tiers, in one email, ordered so the easy "yes" is first:**

1. **Permission to name** IRIS Software Group / IRIS Elements in describing where the design came
   from. Lowest-cost yes. Ask for it in writing, and say what it will be used for and where.
2. **A two-sentence quote, cleared for public use**, attributed to a named individual and role, on
   why they standardised on the approach. **Offer a strawman for them to rewrite** — a blank request
   for a quote gets ignored; a draft they can edit in ninety seconds gets answered. Make clear the
   words must end up being theirs.
3. **A short case study or joint post** — mentioned as an *offer*, not a request. This is the stretch
   outcome and should be framed as something you would happily write for their review, not as work
   you are asking them to do.

**What the email must contain to make "yes" easy and "no" cheap:**

- Exactly where the quote would appear (project site, README, launch post) and that final copy will
  be sent for approval before publication.
- That the claim is about **the design and the precursor**, not an endorsement of Benzene, and that
  no commercial relationship will be implied — nobody wants to accidentally underwrite an OSS
  project's marketing.
- That the anonymised version will be used regardless and is already true, so declining changes very
  little. Removing the stakes from a request is the single best way to get it granted.
- An explicit, unembarrassing exit: "if this is awkward for any reason, say so and I'll use the
  anonymised version — no explanation needed."
- The specifics ask (provenance §4.4) folded in gently: roughly when it went live, roughly how many
  teams picked it up. Two numbers turn the story from plausible to concrete.

**What to do with a cleared quote — one asset, eight surfaces, same week it lands:**

site hero / README trust block → L1 and L2 → the r/dotnet and HN body text → the newsletter pitch
emails → the `.NET on AWS` pitch (§7.2), where it changes the pitch from interesting to compelling →
the .NET Foundation application → podcast intros (§6.3) → slide 2 of the talk (§6.5).

**If the answer is no:** nothing in the plan stops. Run the anonymised form, never mention the
company, never hint at it, and do not re-ask later. §3.3.2 exists so that this outcome costs the
campaign one artefact, not one quarter.

### 6.1 Newsletters & aggregators — the cheapest reach we have, and the first *outward* action

| Target | Ask | Notes |
|---|---|---|
| **The Morning Brew** (Chris Alcock, `blog.cwa.me.uk`) | Email the link to E1 and L1. He curates daily and links good .NET content without being asked. | **Needs verification:** the most recent issues surfaced in search are from 2024; confirm it is still publishing before investing. |
| **dotNET Weekly** (`dotnetweekly.com`) | Submit each evergreen post via the site's link-submission flow. | Submission appears to need an account. Site returned 403 to automated fetch — **verify the submission mechanism manually.** |
| **ASP.NET Core News** (`aspnetcore.news`) | Submit E2, E3, L1. | Weekly ASP.NET-focused roundup. |
| **.NET News** (`dotnetnews.co`) | Submit posts as published. | Daily curated .NET content. |
| **Reddit r/dotnet** | Post E1/E2/E3 as *content*, L1 as the launch. | **Needs verification:** read the current sidebar self-promotion rules before the first post — automated search could not retrieve them, and getting the project flagged as spam on the single most valuable community would be a permanent own-goal. Establish a posting history with the evergreen posts before ever posting a launch. |
| **Reddit r/aws, r/csharp** | E1, E2, L2 | r/aws is a genuinely good fit for E1 and the origin story. |
| **Hacker News** | Show HN for L1; E1, L3 and S6 submitted on their own merits. | See the risk note in §9. Launch to r/dotnet *first*, HN second. **S6 is the best HN candidate in the plan** — "how a library spread without a mandate" is an HN-native topic that happens to be about us. |
| **lobste.rs** | E1, L3 | Small, high-quality, allergic to marketing. Only submit the honest/technical posts. |
| **dev.to** | Syndicate every evergreen post with a canonical link back to `benzene.app`. | Free second distribution. Own domain always publishes first. |

**Rev 2 — how the provenance is framed in community posts.** It goes in the **body, never the
title**, and always after the technical substance. A title like "the framework used by a major
accountancy platform" is (a) not true of Benzene, (b) reads as marketing, and (c) invites the
correction that defines the thread. The right shape is: lead with the problem, show the code, and
then — for the reader who is now asking "but has anyone actually run this?" — answer with §3.0's
one-sentence form, limit clause included. **Answering the trust question before it is asked, in a
post whose title never claimed anything, is the strongest position available in a community
thread.** If a commenter presses, the maintainer already has the honest answer written down and does
not have to improvise under pressure.

### 6.2 Communities

- **.NET Discord / C# Discord community servers** — participate genuinely; answer questions in the
  areas Benzene touches (serverless, messaging, testing). Do not drop links. Value accrues over
  months, not weeks.
- **Stack Overflow** — answer real questions about testing Lambda handlers and sharing code across
  Azure Functions/ASP.NET Core. Disclose affiliation every time. High-quality answers that happen to
  mention Benzene at the end age extremely well.

### 6.3 Podcasts — pitch the *story* (L2), never the feature list

**Rev 2 makes this pitch materially better.** "I built an OSS framework" is a weak podcast pitch;
"a library we built for one team spread across a company's estate on its own merits, and here is
what I learned about why" is a real episode. Lead the pitch with that. Two cautions: **brief the
spoken form from §3.0 before recording** — live conversation is exactly where an anonymised claim
gets accidentally named or inflated into "battle-tested", and a podcast cannot be edited after the
fact by us — and if permission has not landed, tell the host in advance that the company is not
named, so they do not press for it on air.

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
  something he might find genuinely useful. No promotion ask. **Rev 2: S6 is the better second
  touch** — internal-adoption dynamics is squarely his subject matter, and it is a post about an
  idea rather than a product.
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

**Rev 2: this pitch gets substantially stronger, and the reason is worth stating precisely.** The
precursor **runs in production on AWS, in a real commercial accountancy platform, and spread across
teams there.** That is not a framework pitch — it is an *AWS Lambda customer-outcome story*, which
is exactly the category the `.NET on AWS` blog exists to publish. It moves the pitch from "here is
an OSS project, would you write about it" to "here is a real production Lambda architecture at
scale, and the .NET pattern that came out of it." Those are different conversations with different
hit rates.

Three honest consequences:

- **The AWS pitch is the one artefact where the name genuinely matters** (§3.3.2). An anonymised
  production story is publishable but far less interesting to a vendor, and AWS's own editorial
  process will likely want the customer's sign-off in addition to ours. Plan it as **double-gated**:
  a stretch outcome, not a milestone.
- **Timing holds at T0 + 3 months** despite the stronger story. The pitch still needs published
  content behind it, and pitching before the blog programme exists wastes the one good approach we
  get. The exception: **if a cleared, named quote lands early, move the AWS pitch to the front of
  the Phase 3 queue** — it is then the single highest-value outreach action available.
- **This sharpens §7.4's asymmetry rather than softening it.** Our strongest trust asset is an
  AWS-production asset. That is another reason portability stays a closing bonus line and never the
  lead — it is not vendor appeasement, it is that the *evidence we actually have* is AWS-shaped, and
  the campaign should lead with the claim it can best support.

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

**Rev 2 caution on the Azure story.** The provenance does **not** transfer here, and must not be
allowed to leak into Azure-facing copy by implication. The precursor ran on AWS; the Azure surface
is part of the "new and unproven" half of §3.0. An Azure post that opens with "a design proven in
production" and then shows Azure triggers is technically parseable and practically misleading — do
not write it. The Azure story stands on the wedge and the trigger matrix alone, which is enough.
Separately, the provenance **does** strengthen the .NET Foundation application, where "the design
has a production history" speaks directly to the project-longevity criterion the committee cares
about.

### 7.4 The independence trade-off — stated openly

**Vendor association buys reach and institutional credibility; it costs message control, and it can
make the other vendor's community cooler on you.** Benzene's position is genuinely awkward here,
and I would rather name it than paper over it:

- **The portability claim cannot go in a vendor's blog.** No AWS or Microsoft venue will publish
  "and you can leave when you want to." That is fine — §3.4 already demotes portability to a closing
  bonus line. But it means vendor content is a *subset* of our story, never the whole of it.
- **The rule: each vendor gets the story that is true on their platform, and never a story that is
  untrue anywhere.** AWS gets consolidation-and-testability. Azure gets transport-mixing. Both are
  fully honest; neither is the complete picture; the complete picture lives on `benzene.app`.
- **New in rev 2 — the vendor-editorial overclaim pressure.** A vendor blog wants a strong customer
  story, and their editors will reach for stronger language than we can support ("proven at scale",
  "powering"). **§3.0's rules apply inside a vendor post exactly as they do on our own site: the
  limit clause travels, or the post does not happen.** Say this up front in the pitch rather than
  discovering it in review — it is easier to set the register at the start than to walk copy back,
  and an editor told early will usually respect it.
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

### Phase 0 — Foundation (6 weeks, pre-tag) — **~43 hours (~7 h/week)** *(rev 1: ~45)*

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
| ~~Write and publish **E4** (hexagonal in C#)~~ — **moved to Phase 2 in rev 2** | ~~6~~ 0 |
| **Rev 2: close gate 2 (IP/lineage), answer the Digiterre question, draft and send the permission + quote email, one follow-up** | 2 |
| **Rev 2: write the §3.0 trust line into the README trust block and the site hero sub-line** (copy is written; this is placing it) | 2 |
| Pre-write launch-week assets: L1 draft, release notes, newsletter emails, the r/dotnet and HN posts | 5 |

*Zero promotion of Benzene-the-project in this phase.* The evergreen posts go out on their own merits
and start ageing into search. **If hours are short, cut E4 and ship in 5 weeks with three posts.**

**Rev 2 net effect on Phase 0: −2 hours** (45 → 43). E4 moving out pays for the permission work and
the trust-block copy with hours to spare. **E4 is the right thing to displace**: it was already
first on rev 1's cut list, it is the least differentiated post in the programme (hexagonal-
architecture content is a crowded field), and it is the only Phase 0 post whose search value does
not decay by being published two months later.

### Phase 1 — Launch (2 weeks, T0) — **~24 hours, concentrated** *(rev 1: ~22)*

Requires a genuinely cleared week. Everything lands inside 7 days so the surfaces compound.

| Work | Hours |
|---|---|
| Cut the release: bump `version.txt`, tag, publish, GitHub release, drop prerelease badges | 4 |
| Publish **L1**, **L2**, **L3** across the same week | 8 |
| **Rev 2: L2's rewrite as the lineage post** — more structure, more care, and a claims-check against the provenance doc | +2 |
| Submit to newsletters, r/dotnet, HN, lobste.rs, dev.to syndication | 3 |
| **Respond to everything, fast** — this is the phase that converts, and the one people underestimate | 7 |

**Rev 2 net effect on Phase 1: +2 hours** (22 → 24). Worth it: L2 is now the post that answers the
campaign's hardest objection, and it is the one artefact where under-investing shows.

### Phase 2 — Sustain (months 2–5) — **~73 hours (~4.6 h/week)** *(rev 1: ~64)*

| Work | Hours |
|---|---|
| **S1**, **S2**, **S6** (one post per month; **rev 2: S6 replaces S3**) | 24 |
| **Rev 2: E4**, displaced from Phase 0 | 6 |
| **Rev 2: permission-landed contingency** — if the named form or a quote clears, upgrade README, site, L1, L2, the talk and the pitches in place | 3 |
| Podcast outreach + one recorded appearance | 8 |
| User-group CFS submissions + one talk (write once, deliver twice) | 16 |
| Community presence: Discord, Stack Overflow, issue responsiveness | 12 |
| Influencer outreach (§6.6) — 4 personal emails, spaced | 4 |

**Rev 2 net effect on Phase 2: +9 hours** (64 → 73), of which 6 is E4 arriving rather than new work.
The 3-hour contingency is only spent if permission lands — a good problem.

### Phase 3 — Second wave (months 6–8) — **~34 hours (~3 h/week)**

| Work | Hours |
|---|---|
| **S4**, **S5** (S5 = the mesh mini-launch, gated on UI polish) | 16 |
| Vendor approaches: `.NET on AWS` pitch, .NET Foundation application, AWS Community Builders | 8 |
| Conference CFP submissions (NDC London, .NET Conf, DDD) | 6 |
| Second podcast / OSS Power-Ups | 4 |

### Total: **~174 hours over 8 months (~5.1 h/week average)** — rev 1 was ~165

**Rev 2's honest accounting: +9 hours net, of which 6 is E4 rescheduled rather than added, and 3 is
contingent on permission being granted.** The genuinely new work — closing the IP gate, the
permission email, the trust-line copy, L2's deeper rewrite — is **~7 hours, and it is paid for by
moving E4 out of the critical path.** Nothing else was appended without something being moved.

**This is at the edge of what one person can sustain alongside maintaining the library, and I have
already cut to fit.** What was cut, and why, is in §9. If real availability is closer to 2 h/week,
the honest plan is: **do Phase 0 (E1, E2, E3 only, plus the 2-hour permission email — it is the
highest return-per-hour item in the entire plan and must not be the thing that gets dropped), do
Phase 1 in full, then drop Phase 2 to one post per two months and skip user-group talks.** A
campaign that stops after launch is worse than a smaller campaign that keeps going — the drumbeat is
what turns a spike into adoption.

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
| **NEW — whether "has anyone actually run this?" still dominates launch threads** | **Honest, qualitative, and the direct test of whether pillar 2 works.** | Read the r/dotnet and HN threads for it. If the objection is still the top comment despite L2 and the trust line, the framing failed and needs rewriting — not more posting. If it appears and is *answered by another commenter* quoting our own copy, the pillar is working. |

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
8. **NEW — provenance drift.** The most likely way this campaign damages itself is not a deliberate
   overclaim; it is erosion. "The design has production history in a narrower scope" becomes "it's
   been in production for years" becomes "battle-tested", across a podcast, a conference Q&A and
   three months of retelling. Nobody decides to do this. Mitigation: **§3.0's verbatim rule, the
   banned-phrasings list, and a scripted spoken form rehearsed before any live appearance.** The
   maintainer is the only person who can breach this, and live formats are where it happens.
9. **NEW — the IP / clean-lineage gate is a genuine legal risk, not a formality.** Publicising a
   lineage is precisely what prompts someone to ask whether the code was theirs. If the precursor
   was work-for-hire, the *design pattern* is not ownable but the *code* is not the maintainer's to
   reuse. **Gate 2 blocks everything provenance-related, anonymised included** (§3.3.1) — anonymity
   does not help here, because the party who would object already knows who they are. Close it in
   Phase 0 week 1; if it does not close cleanly, take advice and run rev 1's plan meanwhile.
10. **NEW — the "so nobody has actually used it" reading.** A sharp reader will correctly observe
    that this is a claim about a *different, narrower* piece of software. That reading is fair, and
    the answer is not to argue: it is that we said so first, in our own copy, before they did.
    Volunteering the limit converts the strongest available objection into evidence of honesty. The
    failure mode is being *seen to be caught* rather than being asked — which is why the limit clause
    is non-negotiable and why L3 carries a "what is and isn't proven" section.
11. **NEW — relationship risk with the former employer.** A badly-judged ask, or naming without
    permission, could damage a relationship that matters to the maintainer personally and cost the
    campaign its best asset permanently. Mitigation: one email, one follow-up, an explicit easy exit,
    never a third chase, and never a hint of the name if the answer is no or silence (§6.0).

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
- **No case studies or testimonials** — there are no Benzene users yet, and inventing them is out of
  the question. **Rev 2 nuance:** a *precursor* case study is a legitimate stretch goal (§6.0), but
  it is a case study about the design's history, must be labelled as such, and does not become a
  "Benzene customer story" under any circumstances.
- **No "trusted by" logo strip, no company logos anywhere.** A logo implies endorsement or a
  commercial relationship; we have neither, and it is the fastest way to turn a true story into a
  false impression. This holds even if permission to name is granted — permission to *name* in prose
  is not permission to imply *endorsement* in a logo strip.
- **No provenance in titles, headlines, sub-heads, tweets or NuGet summaries.** Formats too short to
  carry the limit clause do not carry the claim (§3.0, rule 2).
- **No management/procurement content track for 1.0.** The `why.html` site page covers audience D;
  a dedicated track needs evidence we don't have.
- **No Medium, no Dev.to-first publishing.** Own domain publishes first, always; dev.to is
  syndication with a canonical link.
- **No Google Cloud or Cloudflare messaging.** Both are explicitly out of 1.0 scope
  (`1.0-release-plan.md` §1) and marketing them would contradict the release's own honesty.
- **No mesh at launch, no cross-language spec as a theme.** See §3.4.
- **No astroturfing, no sockpuppets, no vote manipulation, no mass unsolicited outreach.** Ever.

---

## 10. Immediate next actions

Ordered. **Rev 2 reorders this list**: actions 1–2 are new and go first, because they have the
longest lead time and the widest blast radius, and because both are cheap.

1. **Close the IP / clean-lineage gate (provenance §4.2) — this week, before writing any provenance
   copy.** Confirm Benzene is an independent implementation of the design rather than derived code,
   and answer the Digiterre question in §1.1 (who employed you to build the precursor, and does the
   2023 published report describe the same engagement). **This gates the entire trust pillar,
   anonymised included.** *(Maintainer only; ~1 hour, possibly a contract re-read.)*
2. **Send the permission-and-quote email to the chief architect** (§6.0) — three tiers in one
   message: permission to name, a two-sentence cleared quote with a strawman offered for them to
   rewrite, and a case study mentioned as an offer. One follow-up at three weeks, then stop. **Send
   it in Phase 0 week 2, not later** — it is the only item in this plan whose clock is controlled by
   someone else, and everything else proceeds anonymised while it runs. *(~2 hours including the
   follow-up.)*
3. **Set the 1.0 tag date** and work the calendar backwards from it. Phase 0 is six weeks; nothing in
   Phase 1 happens a day before the tag. *(Maintainer decision — blocks the whole plan.)*
4. **Stand up a blog on `benzene.app`.** The campaign spine currently has no home:
   `website/generator/` has no blog concept. Scope: a post list, a post page reusing the existing
   `Layout.cs` shell, markdown-sourced from a `blog/` directory, an RSS feed (newsletters consume
   RSS). *(Route to the website owner; ~6 hours.)*
5. **Export a raster logo from `Logo.cs`** and wire `PackageIcon` centrally in
   `src/Directory.Build.props` — `1.0-release-plan.md` Tier 0.7 already flags it as the one missing
   piece of NuGet polish. Same asset becomes the social card and the talk title slide. *(~3 hours.)*
6. **Fix the `Benzene.Tools` doc bug** in `docs/testing-benzene.md` (line 49) and
   `docs/cookbooks/testing-lambda-functions.md` — the correct package is
   `Benzene.Aws.Lambda.Core.TestHelpers`. **Hard prerequisite for E2**, and a live overclaim in the
   docs a launch will drive traffic to. *(Route to core/DX owner; ~30 minutes.)*
7. **Write E1 — the per-transport failure-semantics reference table.** Highest-confidence asset in
   the plan, ~80% already written in `docs/capability-matrix.md`, and it is the post that gets picked
   up on its own merits with no reputation behind it. *(~8 hours.)*
8. **Capture the visual assets that do not exist.** There is not a single screenshot in the repo.
   Needed before any post or talk: the Mesh Explorer demo, the Spec UI demo, and
   `MeshServiceWiring.Configure`/`CreateOrderTest.cs` as code images. *(~2 hours.)*
9. **Verify the five "needs-verification" items** before relying on them: which domain is actually
   serving the site, r/dotnet's current self-promotion rules (read the sidebar), The Morning Brew's
   2026 activity, dotNET Weekly's submission mechanism, and the .NET Foundation's precise Activity
   requirements. *(~1 hour total.)*
10. **Draft the launch-week runbook** — the exact order of the tag, the three posts, the newsletter
   emails and the community submissions, written down before launch week rather than improvised
   during it. **Rev 2: it opens with a gate check — which form of the trust line (§3.0) is cleared as
   of launch day, named or anonymised — so that decision is made once, in writing, and not
   re-litigated in each artefact at 11pm.** *(~2 hours.)*
11. **Send one relationship email now, with no ask:** Derek Comartin (CodeOpinion), sharing E1 once
   published. The best time to start a relationship is months before you need it.

---

## 11. Related documents

- [`work/website-marketing-aims.md`](website-marketing-aims.md) — messaging pillars; §7b's
  repositioning is the direct basis for §3.2's wedge
- [`work/website-audience-plan.md`](website-audience-plan.md) — the four audiences behind §3.1
- [`work/benzene-vision.md`](benzene-vision.md) — the philosophy every claim must stay honest to
- [`work/1.0-release-plan.md`](1.0-release-plan.md) — the authoritative launch state behind §1.2
- **[`work/benzene-production-provenance.md`](benzene-production-provenance.md) — SOURCE OF TRUTH for
  §3.0's trust line, §3.3's gates and §6.0's ask. Re-read it before writing any provenance copy; if
  this plan and that document ever disagree, that document wins**
- [`work/enterprise-adoption-gap-analysis.md`](enterprise-adoption-gap-analysis.md) — the objections
  a tech lead will raise
- [`docs/capability-matrix.md`](../docs/capability-matrix.md) — the honest boundaries; source for E1,
  L3 and S2
- [`docs/testing-benzene.md`](../docs/testing-benzene.md) — source for E2's central claim
- [`work/service-mesh-roadmap-1.0.md`](service-mesh-roadmap-1.0.md),
  [`work/mesh-ui-product-vision.md`](mesh-ui-product-vision.md) — the Phase 3 mesh mini-launch
</content>
