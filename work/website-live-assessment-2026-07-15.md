# Benzene Website — Live Site Assessment (2026-07-15)

**Status:** Complete, first-time-visitor audit
**Owner:** Developer Experience (DX) Champion
**Scope:** `http://www.golambda.co.uk` as a first-time visitor deciding whether to adopt Benzene
**Companion docs:** `work/website-marketing-aims.md` (the messaging goals checked against here),
`website/CLAUDE.md` / `website/README.md` (how the generator works)

---

## 0. A methodology caveat — read this before the findings

**This environment's network egress does not allow the live host.** Every attempt to fetch
`http://www.golambda.co.uk` failed identically:

- `WebFetch` on `http://www.golambda.co.uk` → `403 Forbidden`.
- `curl -sSv http://www.golambda.co.uk/` → `HTTP/1.1 403 Forbidden`, `x-deny-reason:
  host_not_allowed`, body `Host not in allowlist: www.golambda.co.uk. Add this host to your
  network egress settings to allow access.`
- The bare domain `golambda.co.uk` (no `www`) doesn't even resolve (`ENOTFOUND` /
  `Could not resolve host`).

So **I did not browse the live site directly**, and nothing below should be read as "I clicked
this on golambda.co.uk and saw X." Instead, I built the exact same generator against the current
`docs/`/`website/` tree on `main` (`dotnet run --project website/generator -- --out …`, from
commit `6163c03`, which `git rev-parse origin/main` confirms is the tip of the actual remote
`main` branch), which is the identical input `.github/workflows/deploy-website.yml` feeds to `aws
s3 sync`. That gives a faithful proxy for **content, links, and messaging** — everything the
generator itself produces and its self-check can (and can't) catch — but it cannot tell me:

- **Whether the live bucket is actually in sync with this `main` HEAD** (i.e. whether
  `deploy-website.yml` has run and succeeded since the `903461a`/`6163c03` messaging-correction
  commits landed). Recommend checking the Actions tab for the latest `Deploy Website` run.
- Real MIME types served by S3 for `.css`/`.svg` (a classic static-hosting footgun where CSS gets
  served as `text/plain` and silently fails to apply).
- Real mobile rendering in an actual browser (I inspected `site.css`'s media queries and DOM
  order only — flagged below as structural risk, not a visual finding).
- Whether the S3 static-website "index document" setting correctly serves `docs/index.html` for a
  request to `/docs/` (trailing slash, no filename) — this is bucket configuration done outside
  the repo per `website/README.md`, not something the generator controls.

One fact *was* independently verified over the open network, outside the blocked host: **`https://www.nuget.org/packages/Benzene/` returns a real HTTP 404** (see finding #2). That's a
live, verifiable fact, not a build artifact.

If the live site's deploy is current, every content/link finding below applies to it as written.

---

## 1. First impression — does the pitch land? **Yes, and it's a genuine improvement over "just multi-cloud."**

The hero tagline (`index.html`, rendered from `MarketingContent.Tagline`) reads:

> "One codebase. Every cloud. Every transport. Write your message handlers once, then run the
> same service on AWS, Azure, Google Cloud, Cloudflare, Kubernetes, or a plain ASP.NET Core host
> — **and swap between HTTP, Lambda, SQS, Kafka, and more with minimal reconfiguration, not a
> rewrite.**"

That single sentence hits both pillars from `work/website-marketing-aims.md` §2 in the first 10
seconds — runs-everywhere *and* the (more important) transport-swap claim — and the four
"Why Benzene?" feature cards lead with **"Swap transports, not code"** first, with **"Runs
anywhere you need it to"** deliberately fourth. That ordering matches marketing-aims.md §2.2's
explicit instruction to foreground the transport-swap pillar at least as prominently as
runs-everywhere, not let it get buried under the easier "multi-cloud" claim. The `<meta
name="description">` (what shows in a Google/search snippet) makes the same two-pillar point in
one sentence too. This part of the site is doing its job — it does not read as "yet another
multi-cloud framework."

The corrected platform list (`MarketingContent.Platforms`, `website/generator/MarketingContent.cs:68-76`)
matches marketing-aims.md exactly: AWS / Azure / Google Cloud / Cloudflare / Kubernetes / Virtual
machines-self-hosted, with Kafka correctly demoted to "a thing the VM/self-hosted target
consumes," not its own platform row. Good — the corrections described in
`work/website-marketing-aims.md` §3 are present in the generator source as claimed.

**Verdict on this stage: SMOOTH**, contingent on the live deploy actually reflecting this source
(unverifiable here, see §0).

---

## 2. Findings, prioritized

### Finding 1 — Homepage's own quickstart snippet won't work if copy-pasted literally
**Stage:** Discover & decide / Build first service
**Severity:** High

The homepage's first code sample (`website/generator/MarketingContent.cs:40-50`, rendered
verbatim into `index.html`'s "Quickstart" section) is:

```csharp
[Message("hello:world")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldMessage, HelloWorldResponse>
{
    public Task<IBenzeneResult<HelloWorldResponse>> HandleAsync(HelloWorldMessage message)
    {
        return Task.FromResult(BenzeneResult.Ok(new HelloWorldResponse { Message = $"Hello {message.Name}" }));
    }
}
```

It's missing `[HttpEndpoint("GET", "/hello/{name}")]`. I confirmed in
`src/Benzene.Http/Routing/ReflectionHttpEndpointFinder.cs` that HTTP routes are discovered
*exclusively* from `[HttpEndpoint]` attributes on the handler class (`MapHandlers` iterates
`GetCustomAttributes<HttpEndpointAttribute>()`); a handler with only `[Message(...)]` registers no
HTTP route at all. So a visitor who copy-pastes this exact snippet plus the second snippet
(`Program.cs` wiring, which *does* correctly show `UseHttp(...).UseMessageHandlers()`) gets a
service that builds, runs, and returns nothing for any URL — a silent, confusing dead end, on the
single most prominent code sample on the entire site.

This is also an internal inconsistency: the feature card two sections earlier ("No routing tables
to maintain") explicitly claims handlers are "mapped to their topic **and HTTP route** by
attribute" — but the code directly below only shows the topic attribute. The one-click-away full
walkthrough (`docs/getting-started.md`, "3. Write a message handler") has the correct, complete
version with `[HttpEndpoint("GET", "/hello/{name}")]` present — so the bug is specifically in the
hand-authored homepage teaser, not in the docs.

**Fix (recommended, not applied — this is `MarketingContent.cs` content, in the DX/product-owner
overlap zone your charter asks me to route rather than silently edit given it's shared,
hand-authored marketing copy):** add `[HttpEndpoint("GET", "/hello/{name}")]` above the class in
`website/generator/MarketingContent.cs:42`, matching `docs/getting-started.md`'s version.

### Finding 2 — Every "NuGet" link on the site points to a package that doesn't exist (verified 404)
**Stage:** Discover & decide
**Severity:** High

`website/generator/Layout.cs` hardcodes `https://www.nuget.org/packages/Benzene/` in two places
shared by **every page on the site**:
- Line 60 — the hero badge row (`<img src="https://img.shields.io/nuget/v/Benzene.svg" alt="NuGet">`, wrapped in a link to the same URL), shown only on the homepage.
- Line 180 — the top-nav "NuGet" link, shown in the header of **every single page**, marketing and
  docs alike.

I verified directly over the open network (not through the blocked `golambda.co.uk` host):

```
$ curl -sS -o /dev/null -w "HTTP_STATUS:%{http_code}\n" https://www.nuget.org/packages/Benzene/
HTTP_STATUS:404
```

There is no package literally named `Benzene` — confirmed by grepping `src/**/*.csproj` for
`<PackageId>Benzene</PackageId>` / `<AssemblyName>Benzene</AssemblyName>`: no match. The actual
installable packages are `Benzene.AspNet.Core`, `Benzene.Aws.Lambda.Core`, etc. — there is no
umbrella meta-package.

This is a first-15-minutes credibility hit sitting in the most-clicked spot on the page: a curious
visitor's natural "let me check this out on NuGet" impulse 404s immediately, on every page, not
just the homepage.

**Fix (recommended, not applied — needs a product decision on which package *is* the canonical
"the one to point people at," since there's no umbrella package):** either (a) point the NuGet
link/badge at a specific, sensible entry-point package (`Benzene.AspNet.Core` is the one
`docs/getting-started.md` tells a newcomer to install first), or (b) publish an actual umbrella
`Benzene` meta-package if that's the intended long-term story, or (c) drop the site-wide NuGet
link/badge entirely until there's one true answer. Don't leave it pointing at nothing.

### Finding 3 — Cookbook index links to 9 cookbooks that don't exist (404 on click)
**Stage:** Learn the concepts / Examples
**Severity:** High

`docs/cookbooks/README.md` lists ~25 cookbooks under section headers (Observability, AWS, Azure,
Validation & Error Handling, Data & Persistence, Testing, Cross-Cutting Concerns). Of those, **9
link to files that do not exist anywhere under `docs/cookbooks/`**:

| Link text | href | Exists? |
|---|---|---|
| S3 Event Processing | `s3-event-processing.md` | No |
| API Gateway Custom Authorizers | `api-gateway-authorizers.md` | No |
| Managed Identity for Azure Resources | `managed-identity.md` | No |
| Request/Response Transformations | `request-response-transforms.md` | No |
| Outbox Pattern | `outbox-pattern.md` | No |
| Contract Testing | `contract-testing.md` | No |
| Rate Limiting | `rate-limiting.md` | No |
| Circuit Breaker Pattern | `circuit-breaker.md` | No |
| Request Authentication & Authorization | `auth-patterns.md` | No |

Confirmed against the actual directory listing (`docs/cookbooks/` has exactly 16 real `.md` files,
listed by name against the 25 entries in `README.md`). The generator's own build log shows these
as `warning: unresolved link '...' in docs/cookbooks/README.md` — 9 separate warnings — but the
generator's self-check **does not fail the build on these** (exit code 0), because
`SiteBuilder.RewriteLinks` deliberately treats any link it can't resolve against a crawled page as
"a link to something outside the site" (per `website/CLAUDE.md`: real repo files like SAM
templates and test `.cs` files are supposed to be left as-is) and leaves it untouched rather than
failing. That design is right for links to real repo files; it's silently swallowing a different,
real problem here — these aren't links to real files elsewhere in the repo, they're **placeholder
entries for cookbooks that were never written**, and the self-check has no way to distinguish the
two cases. On the published site, clicking any of these 9 produces a live 404 against whatever the
S3 bucket's error document is (unverified here — see §0).

**Impact:** roughly a third of the cookbook index is a dead end. A newcomer scanning "Available
Cookbooks" for their specific use case (e.g. "Rate Limiting" or "Circuit Breaker Pattern" — two of
the more commonly-searched resilience topics) will click through and hit nothing, with no
indication in the list itself that these are aspirational/unwritten.

**Fix (recommended, not applied — this is a documentation-writer task: either write the 9 missing
cookbooks or remove/mark them, not a website-generator bug):** shortest fix is to either (a) strip
the 9 unwritten entries from `docs/cookbooks/README.md` until they're written, or (b) mark them
explicitly as "planned" with no link (plain text, not an `<a>`), so the self-check's blind spot
stops mattering. Longer-term, the self-check could be tightened to also fail on any
`docs/cookbooks/*.md`-shaped relative link that doesn't resolve to a real page (as opposed to
links to `../../test/...` or `../../examples/...` which are legitimately external to the site) —
that's a generator-code change, filed here rather than applied since it changes the self-check's
behavior/contract.

### Finding 4 — An orphaned, stale, unfinished "Overview" page is live and contradicts the corrected homepage messaging
**Stage:** Discover & decide / Learn the concepts
**Severity:** Medium-High

`docs/Overview.md` is **not referenced anywhere in `docs/index.md`** (confirmed: `grep -i overview
docs/index.md` → no matches), so it's not part of the intentional nav tree at all — it's picked up
purely because `SiteBuilder`'s crawler includes every `*.md` under `docs/`, and any such orphan
gets dumped into a synthesized "More" sidebar group (`SiteBuilder.AppendOrphanedDocsPages`, per
`website/CLAUDE.md`). It renders live at `docs/Overview.html`, reachable from every single docs
page's sidebar under "More" → "Overview".

Its content is the *entire* file:

```markdown
## Multi cloud

Run Benzene services on Azure Functions, AWS Lambdas or Google Functions. Effortlessly swap between different cloud providers.

Run the same service on any of the following.

- Amazon Web Services
  - Lambda
  - EKS
  - Elastic Beanstalk
- Azure
  - Function
  - Web Service
  - AKS
- Google Cloud
  - Function
- Cloudflare
  - Containers
- Kubernetes
- IIS

### How it works
```

Two separate problems in one page:

1. **It's unfinished** — the file ends mid-outline at the `### How it works` heading with
   literally nothing under it. A visitor who scrolls to the bottom sees a heading and then the
   page just stops.
2. **It's stale and contradicts the corrected messaging.** This is exactly the "just another
   multi-cloud pitch" framing that `work/website-marketing-aims.md` was written to correct — it
   never mentions transport-swapping at all, and its platform list is inconsistent with (and
   predates) the corrected one on the homepage: it lists AWS EKS/Elastic Beanstalk, Azure
   Web Service/AKS, and bare "IIS" as separate targets that don't appear anywhere in the current,
   corrected `MarketingContent.Platforms` list, and it's missing Kafka/SQS/SNS/EventBridge/Service
   Bus/Event Hub entirely from the "what runs where" picture.

Since this page isn't linked from `docs/index.md`, most readers following the intended nav path
will never see it — but it's one click away from *every* docs page (sidebar → More → Overview),
discoverable by anyone who scrolls the full sidebar, and equally discoverable by a search engine
crawling the sitemap (nothing marks it `noindex`). A prospective adopter who lands here via search
(a very plausible "Overview" search-result-worthy title) gets the outdated, weaker pitch instead of
the corrected one — directly undermining the work already done in `work/website-marketing-aims.md`.

**Fix (recommended, not applied — a content decision: delete vs. finish vs. fold into
`docs/index.md`):** given the corrected messaging already lives on the homepage and in
`docs/index.md`'s own intro paragraph, the simplest fix is likely to delete `docs/Overview.md`
outright (it predates and is superseded by the marketing homepage) rather than finish or nav-link
it. If there's a reason to keep it, it needs a same-day rewrite to match the corrected platform
list and pillar framing, and an explicit link from `docs/index.md` (or exclusion from the crawl via
`SiteBuilder.ExcludedFiles`, the same mechanism used for `docs/plans/`).

### Finding 5 — Docs home page's browser tab / search-snippet title reads "Benzene - Benzene"
**Stage:** Discover & decide
**Severity:** Medium (Polish-leaning, but visible on every browser tab and every search result for the docs home)

`Layout.cs:130` renders every docs page's `<title>` as `{Html(title)} - Benzene`, and
`SiteBuilder.cs:44` derives `title` from the page's own first Markdown H1
(`MarkdownText.FindTitle(document) ?? Path.GetFileNameWithoutExtension(sourcePath)`).
`docs/index.md`'s own H1 is `# Benzene` (confirmed: rendered as `<h1 id="benzene">Benzene</h1>` in
`docs/index.html`), so the generated `<title>` for the entire docs section's home page is literally:

```html
<title>Benzene - Benzene</title>
```

That's what shows in a browser tab and in a Google search result for `docs/index.html` — reads
like a copy-paste mistake, not a deliberate design.

**Fix (recommended, not applied — trivial either way, product's call on which):** either rename
`docs/index.md`'s H1 to something like "Documentation" (would also improve the sidebar/breadcrumb
story generally), or special-case the docs-home title in `Layout.RenderDocsPage`/`SiteBuilder` to
render just `Benzene Docs` instead of duplicating the site name.

### Finding 6 — Docs pages link to real repo source files that 404 on the published site (by design, but with no visual warning)
**Stage:** Learn the concepts / Cookbooks
**Severity:** Medium

Several cookbooks and reference pages link to real, existing repo files that aren't part of the
published static site — e.g. `docs/cookbooks/redis-caching.md` links to
`../../test/Benzene.Core.Test/Cache/Redis/RedisCacheServiceTest.cs` and
`../../examples/Azure/docker-compose.yaml`; `docs/clients.md` links to
`../test/Benzene.Core.Test/Clients/OutboundHeaderForwardingTest.cs`; `docs/getting-started-aws.md`
links to `../examples/Aws/Benzene.Examples.Aws/template.yaml`. Per `website/CLAUDE.md`, this is
**deliberate** — only image extensions get vendored as site assets, and these deliberately stay as
unresolved relative links "rather than being copied into the published site." That's the right
call for a lightweight static site.

But the practical effect for a website visitor (as opposed to a GitHub reader, where these same
relative links resolve correctly against the repo tree) is a dead link: on the published site
these render as ordinary in-line text links indistinguishable from a working docs link, and
clicking one takes a visitor to e.g. `http://www.golambda.co.uk/test/Benzene.Core.Test/Cache/Redis/RedisCacheServiceTest.cs`,
which 404s. Nothing on the page (no icon, no "view on GitHub" affordance, no `↗` external-link
marker) distinguishes "this points into the site" from "this points into the repo and only works
on GitHub."

**Fix (recommended, not applied — a generator-behavior change, more than a doc typo):** rewrite
these specific unresolved links to absolute `github.com/daniellepelley/Benzene/blob/main/...`
permalinks at generation time (so they still work from the website domain) instead of leaving them
as repo-relative paths that only resolve on GitHub. This is squarely a `SiteBuilder.RewriteLinks`
change, not something to patch per-doc.

### Finding 7 — `docs/index.md`'s own "Cookbooks" section undersells how much content exists
**Stage:** Learn the concepts
**Severity:** Medium

`docs/index.md`'s "Cookbooks" section (rendered on `docs/index.html`) lists exactly two entries —
"Cookbook Index" and "Logging to Application Insights" — while there are **16 real cookbook pages**
under `docs/cookbooks/`. The other 14 only surface via the auto-generated "More" sidebar catch-all
(`AppendOrphanedDocsPages`), alongside genuinely miscellaneous orphans like `docs/Overview.md`
(Finding 4) and the specification sub-pages. A reader skimming `docs/index.html` — the page the
top-nav "Docs" link and the homepage's "Read the docs" CTA both point to — comes away thinking
Benzene has one or two cookbooks, not sixteen, unless they notice the "More" group at the bottom of
the sidebar and think to look there.

**Fix (recommended, not applied — a documentation-writer task, `docs/index.md` content, not
generator code):** either list all 16 real cookbooks under the "Cookbooks" heading in
`docs/index.md` (matching what `docs/cookbooks/README.md` already enumerates), or link
`cookbooks/README.md`'s full index more prominently as "see all 16 cookbooks →" so the docs home
page's summary isn't misleadingly thin.

### Finding 8 — Minor naming inconsistency in the homepage's quickstart type names
**Stage:** Build first service
**Severity:** Polish

The homepage snippet uses `HelloWorldMessage` as the request type name
(`IMessageHandler<HelloWorldMessage, HelloWorldResponse>` /
`HandleAsync(HelloWorldMessage message)`), while the full walkthrough one click away
(`docs/getting-started.md`) — the page this exact scenario is meant to lead into — uses
`HelloWorldRequest` for the same concept. Not independently a blocker, but it's exactly the kind of
naming-inconsistency papercut the project's own `CLAUDE.md` calls out as something that "forces
re-learning and erodes trust." A newcomer bouncing between the homepage teaser and the full guide
notices the type name changed and has to re-verify whether that's meaningful.

**Fix (recommended, not applied — trivial one-line rename):** rename `HelloWorldMessage` →
`HelloWorldRequest` in `website/generator/MarketingContent.cs:43,45` to match
`docs/getting-started.md` exactly.

### Finding 9 — Mobile layout risk: full sidebar nav renders before page content, with no collapse (structural inference only, not visually verified)
**Stage:** Discover & decide / Learn the concepts (mobile)
**Severity:** Polish/Medium — flagged as a risk to verify in a real browser, not asserted as confirmed

I could not visually test a narrow viewport (no browser available), so this is inferred from
`site.css` and the HTML structure only. In every docs page, `<nav class="sidebar">` (the full,
multi-group nav tree — ~9 top-level groups, 60+ links) appears **before** `<main class="content">`
in the DOM (`docs/index.html:23-24`, confirmed same order on every generated docs page). The
site's only sub-800px media query (`site.css:306-309`) does:

```css
@media (max-width: 800px) {
  .layout { flex-direction: column; }
  .sidebar { position: static; max-height: none; width: 100%; }
}
```

With `flex-direction: column` and no reordering (no `order:` property, no `<details>`/`<summary>`
collapsible wrapper, and the site is deliberately zero-JS per `website/README.md`), the DOM order
above becomes the visual order below 800px: **the entire nav tree renders above the page content**.
A phone user opening any docs page — including the very first thing "Get started in 5 minutes"
links to — would have to scroll past dozens of nav links before reaching the actual guide.

**Fix (recommended, not verified/applied — needs an actual mobile check before committing to a
specific fix):** the cheapest CSS-only fix is `.sidebar { order: 2 }` / `.content { order: 1 }` (or
equivalent) inside the existing `@media (max-width: 800px)` block, so content renders first and the
nav follows, without needing JS or a collapsible widget. Recommend visually verifying on a real
device/browser before landing this, since I'm inferring from source only.

---

## 3. What a "landing → running service" walk-through actually looks like (task item 4)

Following the intended path (not the broken teaser in Finding 1): Home → "Get started in 5
minutes" → `docs/getting-started.html` → the guide there is complete, accurate, and — per the
already-completed `work/dx-roadmap-1.0.md` audit — was previously verified to actually compile and
run end-to-end (`{"message":"Hello world!"}` reproduced). That path is genuinely smooth. The
friction is concentrated in the places a newcomer would naturally *also* poke around while
deciding whether to commit: the homepage's own teaser snippet (Finding 1), the NuGet link every
page invites you to click (Finding 2), and the cookbook/orphan-page dead ends someone browsing
docs for their specific use case would hit (Findings 3, 4, 6). None of these block the single
straight-line "5 minutes" path; all of them are exactly the kind of exploratory clicks a genuinely
curious evaluator makes in their first 15 minutes, and all of them currently dead-end.

---

## 4. Deployment-artifact vs. content/UX distinction (task item 5)

Everything in §2 is a **content/UX problem** traceable to a specific source file
(`docs/*.md`, `website/generator/*.cs`) — none of it is a deployment artifact bug, because I built
from source and the same bugs would appear in any correctly-deployed copy of this exact source.

I found **no evidence of a deployment-artifact-class bug** (stale content vs. `main`, wrong MIME
types, missing assets) — but I also could not check for one, since that requires hitting the live
host, which this sandbox's network egress blocks (§0). The one thing worth a human checking
directly (not deducible from source) is: **does the live bucket currently reflect commit `6163c03`**
(today's messaging corrections), or is it serving an older, pre-correction build? If
`deploy-website.yml`'s last run predates today's commits, the live site right now may still have
the *original* uncorrected platform list/messaging that `work/website-marketing-aims.md` describes
fixing — which would mean Finding 1 (missing `[HttpEndpoint]`) is the *only* one of these findings
guaranteed to already be live, since it predates today's changes entirely.

---

## 5. Prioritized punch list

1. **[High]** Add `[HttpEndpoint("GET", "/hello/{name}")]` to the homepage's quickstart handler
   snippet (`website/generator/MarketingContent.cs:42`) — currently the single most-copied code
   sample on the site doesn't work as shown.
2. **[High]** Fix or remove the site-wide "NuGet" link/badge (`website/generator/Layout.cs:60,180`)
   — `nuget.org/packages/Benzene/` is a confirmed 404; point it at `Benzene.AspNet.Core` or an
   equivalent real package, or drop it.
3. **[High]** Strip or clearly mark the 9 unwritten cookbook entries in `docs/cookbooks/README.md`
   (s3-event-processing, api-gateway-authorizers, managed-identity, request-response-transforms,
   outbox-pattern, contract-testing, rate-limiting, circuit-breaker, auth-patterns) — currently
   live dead links.
4. **[Medium-High]** Delete or rewrite `docs/Overview.md` — stale, unfinished, contradicts the
   corrected homepage messaging, and is one click away from every docs page via the "More" sidebar
   group.
5. **[Medium]** Fix the docs-home `<title>` duplication ("Benzene - Benzene") —
   `website/generator/Layout.cs:130` / `docs/index.md`'s H1.
6. **[Medium]** Rewrite doc links to real repo source files (`.cs`/`.yaml`) as absolute GitHub
   permalinks in `SiteBuilder.RewriteLinks`, instead of leaving them as repo-relative paths that
   404 on the published site.
7. **[Medium]** Expand `docs/index.md`'s "Cookbooks" section to list (or clearly point to) all 16
   real cookbooks, not just 2.
8. **[Polish]** Rename `HelloWorldMessage` → `HelloWorldRequest` in the homepage snippet to match
   `docs/getting-started.md`.
9. **[Polish/Medium, unverified]** Verify mobile rendering in a real browser; if the sidebar
   renders above content below 800px as the CSS suggests, add an `order` rule to put content first.
10. **[Info]** Confirm (via the Actions tab, not deducible here) that the latest `Deploy Website`
    run is green and postdates commit `6163c03`, so the live site actually reflects today's
    messaging corrections rather than an older build.

---

## Verdict

**Messaging/positioning (task item 1): SMOOTH.** The corrected two-pillar pitch genuinely lands in
the first 10 seconds and doesn't read as "just another multi-cloud framework."

**Docs navigation and page rendering (task item 2, as built from source): ROUGH (issues found, not
yet fixed — filed above, not applied, since these are shared marketing copy / doc-content
decisions outside a "just fix it" scope for a single-pass audit).** The straight-line
getting-started path works; the exploratory paths a curious evaluator takes in their first 15
minutes (homepage snippet, NuGet link, cookbook browsing, sidebar "More" group) currently dead-end
in four separate, independently-confirmed places.

**Live-site verification itself: NEEDS WORK — blocked by sandbox network egress, not by the site.**
Nothing here should be taken as "I confirmed this is live" beyond the one item verified over the
open network (Finding 2's `nuget.org` 404). Recommend re-running this audit (or at minimum
re-fetching the homepage, `docs/getting-started.html`, and `docs/cookbooks/README.html`) from an
environment that can actually reach `www.golambda.co.uk`, to confirm the live bucket matches what's
assessed here.
