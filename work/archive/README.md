# work/archive

Actioned working documents — plans, proposals, reviews and feedback rounds whose work shipped.
They are records, not requirements: nothing in here asks for anything. Each file carries an
`> ARCHIVED <date>` stamp naming the evidence, and this index says in one line what each was and
where its substance went. Flat, filenames preserved; the one deviation is `repo-split/`, kept as a
directory because its script and overlay inputs belong together. Append entries; never rewrite
others'.

| File | What it was | Archived | Where the substance went |
|---|---|---|---|
| `benzene-naming-principle.md` | The naming-principle ruling (ACCEPTED 2026-07-25, incl. the §3c reversal) | 2026-08-20 | `docs/specification/wire-contracts.md` carries the rule |
| `error-payload-proposal.md` | Investigation/proposal for a standard error payload | 2026-08-20 | `docs/specification/wire-contracts.md` §1.3 mandates RFC 9457 |
| `spec-review-2026-07-25.md` | The maintainer's review of the spec draft (9 items) | 2026-08-20 | Settled in the spec; item 1 (CancellationToken) extracted to `work/remaining-issues-plan.md` |
| `mesh-feedback-round-2026-08-16.md` | Persona feedback round 1 | 2026-08-20 | Absorbed into `work/mesh-ui-product-vision.md`'s dated blocks; distilled by `work/mesh-ui-aims.md` |
| `mesh-feedback-round2-2026-08-16.md` | Persona feedback round 2 | 2026-08-20 | Same as round 1 |
| `mesh-feedback-round3-2026-08-16.md` | Persona feedback round 3 (drift/breaking changes) | 2026-08-20 | Same as round 1 |
| `mesh-feedback-round5-2026-08-16.md` | Persona feedback round 5 (deployment coordination) | 2026-08-20 | Same as round 1 |
| `mesh-feedback-round6-2026-08-16.md` | Persona feedback round 6 (round 5 re-test) | 2026-08-20 | Same as round 1 |
| `mesh-feedback-round7-2026-08-16.md` | Persona feedback round 7 (open round) | 2026-08-20 | Same as round 1 |
| `mesh-wave-e-delivery-2026-08-16.md` | Wave E delivery record | 2026-08-20 | Wave shipped; absorbed into `work/mesh-ui-product-vision.md` |
| `mesh-ui-design-simplicity.md` | The simplicity diagnosis/direction for the mesh UI | 2026-08-20 | Superseded by `work/mesh-ui-aims.md`; carried as R6/R8 |
| `mesh-ui-react-assessment.md` | Cost assessment of a React+TS componentisation | 2026-08-20 | Decision executed — benzene-ui is the React+TS library |
| `mesh-mismatch-and-dispatch-plan.md` | Plan: show the schema mismatch, make dispatch real | 2026-08-20 | IMPLEMENTED (benzene-ui `ca9668d`, benzene-dotnet `46f038e`); remainders in `work/remaining-issues-plan.md` |
| `mesh-ui-improvement-plan.md` | Three-wave mesh UI improvement plan | 2026-08-20 | Waves 1–3 DONE (benzene-ui `998f7fa`, `e54351c`, `d2e2093`); open PO decisions in `work/remaining-issues-plan.md` |
| `repo-split-plan.md` | The phase-by-phase repo-split checklist | 2026-08-20 | Phases 1–4 done (this repo has no src/); Phase 5 in `work/remaining-issues-plan.md` |
| `repo-split-manifest.md` | The file-level move/stay manifest for the split | 2026-08-20 | The split it specifies was executed |
| `repo-split/` | Split execution status + overlay files + populate script | 2026-08-20 | The split completed; kept intact as the execution record |
