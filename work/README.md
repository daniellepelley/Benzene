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

- `benzene-vision.md` — the original problem and the design philosophy, cross-language
- `benzene-naming-principle.md` — how Benzene names what it owns on the wire
- `benzene-headers-design.md`, `benzene-headers-plan.md` — the header mechanism
- `error-payload-proposal.md` — whether there is a better error payload, and a standard to adopt
- `cloudevents-design.md` — CloudEvents as a wire format
- `spec-review-2026-07-25.md` — the maintainer's review of the specification draft
- `mesh-ui-product-vision.md` — the shared Mesh UI, which lives in this repo
- `repo-split-plan.md`, `repo-split-manifest.md`, `repo-split/` — how the repositories got this shape
- `website-analytics-setup.md` — turning on traffic monitoring for benzene.app
