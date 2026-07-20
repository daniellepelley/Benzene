# Website Audience Plan — broadening beyond developers

**Status:** Active build (2026-07)
**Purpose:** The current site (`website/`) is excellent but developer-only. This document identifies
the audiences the site must reach, what each needs to see, and how the build satisfies them —
woven into value-themed pages/sections rather than obvious "here's the business page" segmentation.
Companion to `work/website-marketing-aims.md` (the messaging pillars) and `work/benzene-vision.md`
(the engineering philosophy the copy must stay honest to).

---

## 1. The audiences

A framework-adoption decision is rarely one person. Four groups matter, and they read the same site
with very different questions:

### A. Developers (already served)
**Who:** the engineers who'll write handlers day to day, and who usually *discover* the library.
**What they look for:** does it feel good to write? Is it plain C# or a proprietary DSL? How fast to
first success? Testability. Clear docs and examples. Not too much magic. Escape hatches.
**Current coverage:** strong — hero, feature cards, quickstart, docs, demos.

### B. Architects / tech leads
**Who:** decide whether it fits the system and sets a good long-term direction.
**What they look for:** the *shape* of the thing — hexagonal/ports-and-adapters, how transports are
abstracted, how it composes with what they already run, standards compliance (OpenAPI/AsyncAPI,
CloudEvents), extensibility, introspection/service-mapping, and — critically — **what happens when
requirements change** (new transport, new cloud, splitting a service). Lock-in avoidance at the
*transport* level. "Will this box us in?"

### C. DevOps / platform / SRE
**Who:** run it in production and get paged when it breaks.
**What they look for:** reliability and operability — health checks (liveness/readiness,
Kubernetes), observability (tracing, metrics, structured logs, OpenTelemetry), failure handling
(retries/backoff, idempotency, partial-batch failure, DLQ, graceful degradation), deployment story
across hosts, infrastructure-as-code. "Can I see what it's doing and trust it under load?"

### D. Engineering / business management
**Who:** approve the adoption, own the budget and the risk.
**What they look for:** **risk, cost, reliability, longevity.** Reduced vendor/transport lock-in;
lower rewrite cost when the architecture evolves; quality via testability; hiring/onboarding cost
(plain C#, no niche skills); MIT-licensed, no vendor tie; maturity signals (tests, CI, versioning
policy). "What does this save me, and what could go wrong?"

---

## 2. Design principle: themes, not audience labels

The site must **not** read as segmented ("Business page", "Developer page") — that feels like a
sales funnel and alienates the developer who found it. Instead, each audience's questions are
answered inside **value-themed** pages and sections whose framing is a capability or benefit, not a
job title. An architect and a CTO both read "Architecture" and "Why Benzene"; a developer and an SRE
both read "Operations". Every page is honest and technical enough that a developer isn't embarrassed
to share it upward, and clear enough that a manager isn't lost.

---

## 3. Information architecture (the build)

Keep the current home page as the front door, enrich it with a "beyond the code" section, and add
three value-themed deep-dive pages. Top nav becomes: **Home · Why Benzene · Architecture ·
Operations · Docs · GitHub · NuGet** — all topic labels, no audience labels.

### Home (enriched)
Keep hero + "Why Benzene?" cards + core-idea diagram + quickstart + platforms + live demos. **Add**
a "Built for production, not just prototypes" section: three cards that tease the deep-dive pages
(reliability/ops, architecture fit, the business case) so every audience finds their thread within
one scroll. Add light trust signals (MIT, .NET 10, test-first, CI) near the footer CTA.

### `why.html` — "Why Benzene" (primary: management; also architects)
The value case, framed as "why choose this," never "for executives":
- **Lower the cost of change** — the same handler moves across transports/hosts as wiring, not a
  rewrite; concrete "what a new event source costs you" contrast.
- **Reduce lock-in & risk** — transport- and vendor-agnostic; plain C#, no proprietary runtime;
  MIT license; your logic isn't hostage to one cloud's event model.
- **Reliability you can point to** — health checks, retries, idempotency, observability (link to
  Operations).
- **Quality by construction** — test-first tooling means handlers are unit/integration tested
  without cloud emulators (link to testing docs).
- **Cheaper to staff** — it's C# and DI your team already knows; no niche framework skills.
- **Built to last** — semantic-versioning policy, migration guides, broad .NET support.

### `architecture.html` — "Architecture" (primary: architects; also senior devs)
- **Ports & adapters, honestly applied** — what a handler/transport/middleware actually is; the
  hexagon diagram reused.
- **One model, many transports — at once** — the mix-transports story at design depth.
- **Introspectable by design** — OpenAPI + AsyncAPI + EventService specs from code; the service
  map/mesh; contract-drift detection (link demos).
- **Standards & interop** — wire contracts, CloudEvents direction, schema registry.
- **Composable & swappable** — middleware pipeline, DI-container-agnostic (MS DI / Autofac),
  serializer/media-format seams, extension points; "fits your system, doesn't replace it."

### `operations.html` — "Operations" (primary: DevOps/SRE; also management's reliability lens)
- **See everything** — OpenTelemetry traces + metrics, structured correlation-tagged logs,
  per-middleware spans, W3C trace propagation across hops.
- **Health & readiness** — liveness/readiness, Kubernetes, DB health checks.
- **Fail safely** — retries/backoff, Polly resilience, idempotency, SQS partial-batch failure &
  DLQ, ack/nack semantics, timeouts/cancellation.
- **Ship it anywhere** — the hosting matrix, serverless, Terraform/IaC codegen, cold-start notes.

---

## 4. Honesty guardrails

Every claim links to a real docs page or demo. Anything partial/planned is stated as such (per the
`website-marketing-aims.md` §3 precedent — e.g. Kafka is a transport, not a host). No invented
benchmarks; performance is described qualitatively until `benchmarks/` has a citable number
(`work/performance-roadmap-1.0.md`). The capability→page mapping is grounded in an inventory of what
actually ships (health checks, OTel, resilience/Polly, idempotency, SQS partial-batch, mesh/spec
UI, test hosts, Terraform codegen, auth, multi-host) — not aspiration.

## 5. Build mechanics

- Generator gains a small data-driven value-page model (`MarketingPages.cs`) + a
  `Layout.RenderValuePage(...)` that reuses the existing marketing shell (`.section`,
  `.feature-grid`, `.feature-card`). Root-level output paths (`why.html`, etc.); links to
  `docs/*.html`, `demos/*`, and sibling value pages are plain relative hrefs, validated by the
  existing broken-link self-check.
- Header nav extended; `activeSection` generalized to the current page slug.
- No new NuGet deps, no JS. Same S3 deploy path (`deploy-website.yml`).
