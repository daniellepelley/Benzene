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
