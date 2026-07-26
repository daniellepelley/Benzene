# Repo split — execution status

Live status of the autonomous execution of [`../repo-split-plan.md`](../repo-split-plan.md).

## Phase 1 — Stand up benzene-dotnet — PREPARED & VERIFIED, blocked on repo creation

**Blocker (needs the maintainer):** creating the `benzene-dotnet` repo is not possible from this
session — the GitHub integration returns `403 Resource not accessible by integration` on
`create_repository`. **Action needed from you:** create an empty (no README) public repo
`daniellepelley/benzene-dotnet`. Everything else is done and verified.

**What is done and verified:**
- Full move/stay split executed into a staging tree (`git archive` clean snapshot — tracked files
  only, no `bin/obj`, no history).
- Conformance fixtures vendored to `test/conformance-fixtures/` (+ `SPEC_VERSION`, README); the
  `Benzene.Conformance.Test.csproj` fixture path repointed at the vendored dir; the source comment
  updated. This is the **only** code change in the entire MOVE.
- `conformance-drift-check` workflow added (fetches canonical fixtures from benzene, fails on drift).
- Standalone `README.md` / `AGENTS.md` / `CLAUDE.md` written for the .NET-port repo; `docs/index.md`
  spec section replaced with a pointer to the canonical spec in benzene.
- **Verified `dotnet build Benzene.sln` → `Build succeeded, 0 Error(s)`** in the staged tree — proves
  the .NET port is self-contained, nothing references files left behind in benzene.
- **Verified `dotnet test Benzene.Conformance.Test` → 134 passed, 0 failed** against the vendored
  fixtures. The rest of `test/` is byte-identical to benzene (already green in its CI).

**Artifacts (all committed under `work/repo-split/`):**
- `populate-benzene-dotnet.sh` — the exact, reproducible recipe (re-runnable; ends with the push
  commands).
- `overlay/` — the files that differ from their benzene originals (repo docs, drift-check workflow,
  index.md, vendored-fixtures README).

**To finish Phase 1 once the repo exists** (either run the script, or the two push lines):
```
BENZENE=/home/user/Benzene STAGE=/home/user/benzene-dotnet-staging \
  work/repo-split/populate-benzene-dotnet.sh
git -C /home/user/benzene-dotnet-staging remote add origin \
  https://github.com/daniellepelley/benzene-dotnet.git
git -C /home/user/benzene-dotnet-staging push -u origin main
```
(The staging tree at `/home/user/benzene-dotnet-staging` is ephemeral to this container; the script
regenerates it deterministically from benzene HEAD.)

## Phase 2 — Website generator to multi-source — IN PROGRESS
Fully unblocked (all in the benzene repo). Being built against local fixture dirs standing in for the
language repos, so it is verifiable before any cross-repo checkout is wired.

## Phase 3 — Flip deploy-website to multi-checkout — PENDING

## Phase 4 — Cutover (remove migrated content from benzene) — HELD FOR CHECK-IN
Destructive and coordinated (deletes src/test/examples/… from the live repo other sessions push to).
Not run autonomously — needs a maintainer go-ahead and a quiet moment.
