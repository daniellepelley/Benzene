# work

Planning and design notes. Engineering material only — how Benzene works, and why it works that way.

## Moved out: planning and marketing

Documents about **getting Benzene used** rather than about how Benzene works now live in the private
`daniellepelley/benzene-admin` repository. Campaign plans, positioning, audience analysis, adoption
strategy and provenance claims are written for us, about the people we are trying to reach; a public
repository is the wrong place for them.

Moved on 2026-08-08:

| Was | Now |
|---|---|
| `work/marketing-campaign-1.0.md` | `benzene-admin` → `work/marketing-campaign-1.0.md` |
| `work/website-marketing-aims.md` | `benzene-admin` → `work/website-marketing-aims.md` |
| `work/website-audience-plan.md` | `benzene-admin` → `work/website-audience-plan.md` |
| `work/website-live-assessment-2026-07-15.md` | `benzene-admin` → `work/website-live-assessment-2026-07-15.md` |
| `work/enterprise-adoption-gap-analysis.md` | `benzene-admin` → `work/enterprise-adoption-gap-analysis.md` |
| `work/benzene-production-provenance.md` | `benzene-admin` → `work/benzene-production-provenance.md` |
| `.claude/agents/marketing-manager.md` | `benzene-admin` → `.claude/agents/marketing-manager.md` |

The dividing line: **would a reader learn how to use Benzene, or how we intend to sell it?** Honest
engineering limitations stay public — that is what `docs/specification/` and the capability matrix are
for. An honest assessment of how far the project is from being adoptable does not.

Note that these files were public before they moved, so they remain in this repository's git history.
Removing them from `HEAD` is not the same as unpublishing them.

## Archived: superseded status documents and roadmaps

Eleven documents (9,483 lines) were archived to `benzene-admin` → `archive/` on 2026-08-08. All were
superseded by the code-verified release assessment of 2026-07-18; `work/1.0-release-plan.md` is the
successor and remains here.

- **Readiness / API surface** — `1.0-readiness-checklist.md`, `1.0.0-release-checklist.md`,
  `1.0.0-release-status.md`, `1.0-api-readiness-review-2026-07-14.md`, `api-surface-review.md`.
  Each carried a banner in its own text saying not to cite it.
- **Per-area roadmaps** — `aws-`, `azure-`, `google-cloud-`, `dx-`, `observability-`,
  `performance-roadmap-1.0.md`. These carried *no* such banner; the staleness is declared only in the
  release plan that replaced them, so opening one directly gave no warning at all.

**`service-mesh-roadmap-1.0.md` was kept**, despite matching the release plan's blanket
"`*-roadmap-1.0.md`" wording. It is a living document — newest internal update 2026-07-25, a week
after the assessment — owned by the mesh product owner per `.claude/PRODUCT_OWNERS.md`, and cited by
the public `docs/guides/mesh-ui.md`. The blanket judgement is out of date with respect to it.

## Still to sort

Two groups of documents in here are not really this repository's concern either, and are pending a
decision rather than settled:

- **.NET implementation design** — `auth-middleware-design.md`, `batch-failure-handling.md`,
  `client-health-checks-*.md`, `saga-design.md`, `kinesis-batch-failure-handling-design.md`,
  `cloudevents-design.md`, `benzene-clients-*.md`, `designs/`, `spikes/` and others. This repository
  is the language-neutral specification and the shared UI; design notes for the .NET port belong in
  `benzene-dotnet`.
- **Superseded status documents** — several files open by declaring themselves stale
  ("*systematically stale … **Do not cite this***"). A public repository shipping documents that tell
  readers not to trust them is worse than not shipping them.
