# work

Design and planning notes for **this** repository: the language-neutral specification, the shared UI,
the website, and the shape of the repositories themselves.

## One home per document

A document lives in the repository that owns its subject, and other repositories link to it. Two
copies diverge silently, and this project has already proved that twice — the repo split left `work/`
duplicated across `Benzene` and `benzene-dotnet`, and by the time anyone looked, eleven files had
drifted apart. One of them mattered: `benzene-naming-principle.md` recorded the 2026-07-27 reversal of
the topic-header decision here, while the copy in `benzene-dotnet` still described the abandoned
`benzene-topic` spelling.

| Subject | Home |
|---|---|
| The language-neutral contract — wire format, headers, naming, error payloads | **this repo**, `work/` and `docs/specification/` |
| The shared UI and the website | **this repo** |
| The shape of the repositories | **this repo** |
| A language implementation — its packages, APIs, migrations, roadmaps | that port's repo (`benzene-dotnet`, `-go`, `-typescript`, `-python`) |
| Marketing, positioning, adoption strategy, provenance | the private `benzene-admin` repo |

## Living, dated, superseded

Same rules as [`benzene-dotnet/work/README.md`](https://github.com/daniellepelley/benzene-dotnet/blob/main/work/README.md),
which states them in full. In short: a document is either **living** (owned, kept true, citable) or
**dated** (a record of one moment, never updated). Dated documents carry their date and belong in an
archive, not beside the truth — and **nothing in `work/` may tell the reader not to trust it**. That
banner is the symptom; archiving is the fix.

## What moved out, 2026-08-08

**To the private `benzene-admin` repo** — the campaign plan, messaging pillars, audience analysis,
adoption gap analysis, the live-site assessment, the production-provenance record, and the marketing
agent. They are about how we intend to *sell* Benzene rather than how to use it. Eleven superseded
readiness documents and per-area roadmaps went to its `archive/` as well.

**To `benzene-dotnet`** — 41 documents plus `designs/`, `spikes/`, and the dated `arch-review/`,
`bughunt/` and `cloud-review/` passes. Every one was a duplicate of a file already live there, and in
most cases *its* copy was the newer one: `work/` has genuinely been maintained in `benzene-dotnet`
since the split, not here. Nothing was lost by deleting them here; they are one repository away, and
the dated ones are now in [`benzene-dotnet/work/archive/`](https://github.com/daniellepelley/benzene-dotnet/tree/main/work/archive).

Where a document was genuinely about the contract rather than the implementation, the reverse happened
— it stayed here and left `benzene-dotnet`.

Both sets remain in this repository's git history. Removing them from `HEAD` is not the same as
unpublishing them.

> **A note on prose references.** Documents kept here still mention sibling files by name in prose —
> `work/saga-design.md`, `work/service-mesh-roadmap-1.0.md` and so on. Those are not broken links;
> they are references to documents that now live in
> [`benzene-dotnet/work/`](https://github.com/daniellepelley/benzene-dotnet/tree/main/work) (or its
> `archive/`). Clickable links were rewritten to absolute URLs; plain mentions were left as prose.

## What is here

**Kept current as of 2026-08-20.** A stale inventory is how divergence starts — the whole argument
of this file — so this list is checked against `ls work/` when a document is added, not periodically.

### The cross-language contract

- `benzene-vision.md` — the original problem and the design philosophy, cross-language
- `benzene-naming-principle.md` — how Benzene names what it owns on the wire (a ruling, accepted)
- `benzene-headers-design.md`, `benzene-headers-plan.md` — the header mechanism. The plan is ready
  to execute and blocked only on a maintainer go/no-go; its Phase A is a pre-1.0 wire change
- `cloudevents-design.md` — CloudEvents as a wire format. **A decision is owed**: benzene-go has
  shipped a binding that no spec section pins and that diverges from this design
- `ceremony-parity-audit.md` — whether the same capability costs the same amount of code in all
  four ports; the standing record of what has been compared

### Reviews and audits

- `spec-review-2026-07-25.md` — the maintainer's review of the specification draft (open backlog)
- `cross-repo-outstanding-work-2026-08-20.md` — the verified five-repo audit of every
  outstanding-work claim in the documentation, classified against the code
- `remaining-issues-plan.md` — what the overnight quality sweep left open, checked against the live
  registries and workflow runs rather than inferred

### The mesh — product, UI and feedback

The mesh is the largest body of work here, and it splits three ways.

*What it is for, and what it should become:*
- `mesh-ui-product-vision.md` — the Mesh UI's vision and roadmap (living, appended in dated blocks)
- `mesh-ui-aims.md` — the aims each UI element must serve, written as a decision instrument
- `mesh-ui-design-simplicity.md` — the case for simplicity, and the principle to design to
- `mesh-enterprise-readiness.md` — what an enterprise needs from the mesh, researched against code
- `mesh-environments-and-access.md` — who may see a mesh, where it runs, and how the UI is built
- `mesh-versions-and-planning-requirements.md` — service versions, compositions and the planning
  plane, as draft requirements rather than a plan

*Plans and delivery records:*
- `mesh-ui-improvement-plan.md` — the three-reviewer UI review and its waves (waves 1–3 delivered)
- `mesh-ui-react-assessment.md` — what a React componentisation would cost. **Decided and built**:
  it lives in `benzene-ui`
- `mesh-mismatch-and-dispatch-plan.md` — making contract mismatch visible and dispatch real
- `mesh-wave-e-delivery-2026-08-16.md` — what Wave E shipped

*The persona feedback method and its rounds:*
- `mesh-user-personas.md` — the shared brief for the eight persona agents
- `mesh-persona-round-method.md` — how to run a round, and the four ways the harness broke
- The raw evidence packs, in order — `mesh-feedback-round-2026-08-16.md`,
  `mesh-feedback-round2-2026-08-16.md`, `mesh-feedback-round3-2026-08-16.md`,
  `mesh-feedback-round5-2026-08-16.md`, `mesh-feedback-round6-2026-08-16.md`,
  `mesh-feedback-round7-2026-08-16.md`. Rounds 3, 5 and 6 are focused on one question; the rest are
  open. *(Round 4 was a confirmation pass folded into round 5 and has no file of its own.)*

### Transports and tooling under consideration

- `cloudflare-transport-research.md`, `cloudflare-queues-plan.md` — what Benzene could support from
  Cloudflare beyond HTTP, and the work plan for Queues
- `third-party-tool-integrations.md`, `third-party-tool-integrations-plan.md` — observability
  platforms, analyzers, a profile-check GitHub Action and dashboards. WP0 and WP2 are done, WP1 and
  WP3 partly; WP4 onward are unstarted

### The repositories, and the website

- `repo-split-plan.md`, `repo-split-manifest.md`, `repo-split/` — how the repositories got this
  shape. **Executed**, all phases done
- `website-analytics-setup.md` — turning on traffic monitoring for benzene.app. GA4 is live; only
  Search Console is left
- `website-information-architecture-strategy.md` — layering the site so a newcomer meets the
  quickstart before the specification, without thinning any of the reference material

### Archive

- `archive/` — dated records and superseded documents; nothing there is current. See
  [`archive/README.md`](archive/README.md). It currently holds one file:
  `error-payload-proposal-2026-07-25.md`, ruled against and superseded by
  `docs/specification/wire-contracts.md` §1.3
