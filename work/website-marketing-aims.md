# Benzene Website — Marketing Aims

**Status:** Living reference document
**Last Updated:** 2026-07-15
**Purpose:** Capture the marketing positioning and messaging pillars behind the Benzene website
(`website/`), so future landing-page copy changes get checked against a consistent narrative
instead of being decided ad hoc each time. Companion to `work/benzene-vision.md` (the engineering
philosophy this messaging has to stay honest to) — this document is the marketing-facing
translation of that same story, not a competing one.

---

## 1. What the site is for

Two jobs, one site (see `website/CLAUDE.md` for the implementation):

- **Marketing** — the home page (`index.html`), hand-authored, aimed at a C# developer
  evaluating whether Benzene is worth five minutes of their time.
- **Documentation hosting** — everything under `docs/`, rendered as-is from the existing
  `docs/*.md` tree.

Two audiences follow from that:

- **Skimmers** — land on the home page, decide in the time it takes to scroll once whether to
  keep reading. The hero, the four feature cards, and the platform list are aimed squarely at
  this group.
- **Engaged readers** — click through to "Get started in 5 minutes" or the docs nav. Everything
  past that point is the existing docs content, not marketing copy.

---

## 2. The two messaging pillars that are actually true today

These are the two claims the current codebase can honestly back up, and they are **not the same
claim** — the copy needs to make both, not conflate them into one:

### 2.1 Runs everywhere
Benzene ships adapters for a genuinely wide set of hosting targets. As of this doc, that's:
AWS (Lambda, API Gateway, SQS, SNS, Kafka, EventBridge), Azure (Functions isolated worker, Event
Hub, Service Bus), Google Cloud (Cloud Functions, Cloud Run), Cloudflare (Containers), Kubernetes
(any container host, via the self-hosted worker or ASP.NET Core adapter plus
`docs/kubernetes-health-checks.md`'s liveness/readiness support), virtual machines / bare
self-hosted processes (`Benzene.SelfHost`/`Benzene.Kafka.Core`'s own poll loop), and plain
ASP.NET Core embedded in an existing host. See `docs/hosting.md`'s "Three ways Benzene starts"
for the underlying model (triggered/serverless vs. embedded-in-a-host vs. self-hosted worker).

### 2.2 Integrates everywhere, and swaps with minimal reconfiguration
This is the bigger point, and the one the original site copy undersold by treating "runs
everywhere" as the whole pitch. The actual claim is two-part:

1. Benzene talks to a wide range of **transports** (HTTP, Lambda events, SQS, SNS, Kafka,
   EventBridge, Event Hub, Service Bus, gRPC) through the same message-handler abstraction.
2. Changing which transport a handler runs behind is a **hosting/wiring change, not a rewrite** —
   the handler itself (`IMessageHandler<TRequest, TResponse>`) never changes. This is
   `work/benzene-vision.md` §2.1's "a service is defined by what it does, not by its transport"
   principle, restated for a marketing audience.

**Action from this doc:** the "Why Benzene?" feature cards and the platforms section's framing
should foreground claim 2.2 at least as prominently as 2.1 — "runs everywhere" is the easier
claim to picture, but "swaps transports with minimal config" is the actual differentiator versus
a framework that's merely also multi-cloud-deployable.

---

## 3. Correction applied this pass (2026-07-15 review)

Feedback from reviewing the first version of the site:

- **Add Kubernetes** as its own platform entry — it was missing from the original list, and it's
  a real, distinct, commonly-asked-about target (container orchestration, not a specific cloud).
- **"Kafka" is not a place Benzene runs** — it's a transport/broker `Benzene.Kafka.Core` consumes
  *from within* a self-hosted worker process. Listing it alongside AWS/Azure/GCP as a "where it
  runs" entry conflated the two pillars in §2. Replaced with an entry describing the actual
  runtime target (virtual machines / self-hosted process), with Kafka mentioned as one of the
  things that target can consume, not the target itself.
- **Reframed the "runs everywhere" section's lede** to explicitly name the transport-swap
  flexibility as well, rather than leaving it implied by the feature cards alone.

These corrections were applied directly to `website/generator/MarketingContent.cs` (the
`Platforms` array and the "Write once, deploy anywhere" feature card's copy) in the same change
that added this document.

---

## 4. Secondary pillar: performance (not yet a dedicated site section)

Real work has gone into this (`work/performance-roadmap-1.0.md`'s bounded-concurrency worker
dispatch, the per-event DI scope fix, hot-path pipeline design) but there's no benchmark number
yet to put in front of a reader. `benchmarks/Benzene.Benchmarks` exists precisely so any
performance claim on the site can be backed by a real, reproducible number rather than an
unsubstantiated adjective.

**Gate condition:** don't add a "Performance" section to the marketing site until there's a
concrete benchmark result worth citing. An honest "no numbers yet" is better than a vague
"blazing fast" that erodes trust in every other claim on the page.

---

## 5. Future pillar: service mesh visibility (explicitly not on the site yet)

`work/service-mesh-roadmap-1.0.md` covers the actual state (Phase 0-3: health-check dependency
graph, `Benzene.Mesh.Aggregator`, `Benzene.Mesh.Ui`, live Tempo/Prometheus topology). This is a
strong potential differentiator — cross-service visibility purpose-built around Benzene's
message-handler model, not a bolted-on APM — but it is deliberately **not** part of the marketing
site yet.

**Gate condition (explicit, from the person who owns this call):** wait until the mesh code is
more advanced and there's a polished UI to actually show, then add it as its own pillar/section —
ideally with a screenshot or short clip of the mesh UI itself, since "you can see your whole
service graph" is a claim that sells itself far better shown than described.

**When it's ready, revisit:** this doc, `website/generator/MarketingContent.cs` (a new feature
card and/or platform-adjacent section), and possibly a dedicated screenshot/asset in
`website/generator/assets/`.

---

## 6. Current implementation status

| Pillar | Status |
|---|---|
| Runs everywhere (corrected platform list) | Live, this pass |
| Integrates everywhere / minimal-reconfig transport swap | Live, reframed this pass |
| Performance | Not on site — no benchmark numbers to cite yet (§4) |
| Service mesh visibility | Not on site — waiting on mesh UI maturity (§5) |

---

## 7. Next steps

- [x] Add Kubernetes to the platform list
- [x] Replace the "Kafka" platform entry with the actual runtime target (VMs / self-hosted)
- [x] Reframe the "runs everywhere" section to name transport-swap flexibility explicitly
- [ ] Draft a "Performance" section once `benchmarks/Benzene.Benchmarks` has a number worth citing
- [ ] Draft a "Service mesh visibility" section once the mesh UI is more polished — revisit this
      document first to turn §5's gate condition into actual site copy

---

## 8. Related documents

- [`work/benzene-vision.md`](benzene-vision.md) — the engineering philosophy this messaging
  translates; §2.1 in particular ("a service is defined by what it does, not by its transport")
  is the technical basis for §2.2's marketing claim above
- [`work/service-mesh-roadmap-1.0.md`](service-mesh-roadmap-1.0.md) — current state of the mesh
  visibility work gated in §5
- [`work/performance-roadmap-1.0.md`](performance-roadmap-1.0.md) — current state of the
  performance work gated in §4
- [`website/README.md`](../website/README.md), [`website/CLAUDE.md`](../website/CLAUDE.md) — how
  the site is actually built
- [`docs/hosting.md`](../docs/hosting.md) — the three hosting modes referenced in §2.1
