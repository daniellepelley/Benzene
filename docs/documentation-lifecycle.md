# The documentation lifecycle — how docs stop being noise

**2026-08-20.** The rule set that keeps this estate's documentation describing what Benzene *does*
rather than accumulating what we once *planned*. It exists because the working-docs folders grew to
~370 markdown files across six repos, most of them requirements that had already been met — and a
reader could no longer tell the present from the archaeology.

## The three kinds of document

Every markdown file in the estate is exactly one of these, and each kind has one home:

| Kind | What it is | Where it lives | Lifetime |
|---|---|---|---|
| **Spec** | Normative: what a conforming implementation MUST do. Cross-language, RFC-2119 language. | `docs/specification/**` in the main repo only | Living — versioned, never archived |
| **Capability record** | Descriptive: what this code actually does and deliberately does not, package by package. | `docs/capability-matrix.md` in each port; `docs/capabilities.md` (consolidated, all ports) in the main repo | Living — updated as part of every change |
| **Working doc** | A plan, proposal, design, requirements list, feedback round: an intention at a moment. | `work/` in each repo | **Temporary by definition** — archived to `work/archive/` when actioned |

The failure mode this taxonomy prevents: a plan that shipped but stayed in `work/`, indistinguishable
from a plan still owed. Working docs are the only kind allowed to die, and they are *expected* to.

## The standing rule — part of the definition of done

**No piece of work is finished until its documentation has moved through the lifecycle:**

1. The plan/requirement that drove it is stamped and moved to `work/archive/` (or, if partially
   done, its live remainder extracted first). *Agent: `docs-archivist`, or do it by hand — it is
   one `git mv` and a one-line stamp.*
2. The repo's `docs/capability-matrix.md` is updated to state what the code now does or no longer
   does. *Agent: `capability-scribe`.*
3. If the change touched an observable cross-language contract, the spec change lands in
   `docs/specification/**` (the existing AGENTS.md rule — the matrix is not a substitute for the
   spec).
4. If a port's matrix changed, the main repo's consolidated `docs/capabilities.md` is refreshed.
   *Agent: `port-aligner`, main repo only.*

A change that skips these steps has not reduced the noise — it has added to it, because the docs now
disagree with the code in one more place.

## The three agents, and how they compose

| Agent | Scope | Trigger | Reads | Writes |
|---|---|---|---|---|
| `docs-archivist` | any repo | periodic sweep, or when `work/` has visibly outgrown the truth | `work/**`, git history, the source (for evidence) | moves into `work/archive/` + index; fixes references |
| `capability-scribe` | any repo | on every completing change (increment mode); or rebuild when the matrix is stale/missing | the diff and the source — never the plan | `docs/capability-matrix.md` |
| `port-aligner` | main repo only | after any port matrix changes; or on-demand audit | the four ports' matrices + `docs/specification/` | `docs/capabilities.md` |

The composition is a pipeline with one deliberate trust boundary per stage:

- The **archivist** trusts only code and git — never a doc's own "IMPLEMENTED" marker, because docs
  here have been caught claiming both directions falsely.
- The **scribe** trusts only the source — never the plan that requested the work, because plans
  overpromise. Its matrix rows carry the package/path that proves them.
- The **aligner** trusts only the scribes' matrices — never the ports' source, which it does not
  read. A missing or stale matrix produces an `unknown` cell and a named debt, not a guess.

So a fact reaches the consolidated matrix only by surviving two verifications: scribe against
source, aligner against scribe. Divergence between ports is then real signal, in three grades:
*deliberate* (a stated per-port design decision), *staged* (a later port not there yet — normal), or
**drift** (contradiction — the one that pages the product owner).

## Conventions

- `work/archive/` is flat, filenames preserved, `README.md` as a one-line-per-file index. Archived
  docs get a one-line header stamp: when, why, and where the substance now lives. Never delete —
  the archive is evidence.
- Moves fix every inbound reference in the same commit (CLAUDE.md, AGENTS.md, docs, website inputs).
  A cleanup that breaks a link is a defect.
- "Deliberately not" and "not yet" are different statements everywhere: one carries reasoning, the
  other says "unbuilt" plainly. Conflating them is how a matrix loses trust.
- The agent definitions are canonical in this repo (`.claude/agents/docs-archivist.md`,
  `capability-scribe.md`, `port-aligner.md`). The two per-repo agents are vendored into each port's
  `.claude/agents/` the same way conformance fixtures are vendored: copy, don't fork. A port that
  needs to change one changes it here first.

## What this does not try to do

- No automation pretends to *decide* what a port should build — the aligner records divergence and
  grades it; the decision stays with the product owner.
- Blog posts, conformance fixtures, and vendored spec snapshots are history on purpose, in the
  right place; no agent touches them.
- The archive is append-only storage, not a knowledge base. If something in it turns out to still
  matter, the move is: extract into a living doc, leave the archive copy stamped.
