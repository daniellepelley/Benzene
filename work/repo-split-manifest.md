# Repo split — Phase 1 file-move manifest

Companion to **[repo-split-plan.md](repo-split-plan.md)**. This is the exact, reviewable split of
what goes to **`benzene-dotnet`** (MOVE) vs stays in **`benzene`** (STAY), as of the current tree.
Nothing is created or deleted yet — this is the artifact to sign off before Phase 1.

> **Guiding principle (maintainer, confirmed):** this is a **best-endeavors** split, not a
> perfect one. **Duplication across the two repos is acceptable** and gets reconciled over time.
> A **post-split cleanup/review pass is expected** — several of these files (the `.claude/` agents,
> much of `work/`) want a review *anyway* now that the structure is changing — so getting every
> call exactly right up front is explicitly **not** a priority. When a file is ambiguous, make the
> reasonable call, lean toward keeping the .NET repo self-contained, and defer the polish.

Legend: **MOVE** → copied to `benzene-dotnet` (then removed from `benzene` in Phase 4).
**STAY** → remains in `benzene`. **BOTH** → exists in each repo (each gets its own copy).
**SPLIT** → contents divided; see notes.

## Top-level

| Path | Disposition | Notes |
|------|-------------|-------|
| `src/` | **MOVE** | The entire .NET library. |
| `test/` | **MOVE** | All .NET tests. `test/Benzene.Conformance.Test/` moves but is re-pointed at vendored fixtures (below). |
| `examples/` | **MOVE** | All .NET sample hosts/transports. |
| `templates/` | **MOVE** | `dotnet new` template pack. |
| `benchmarks/` | **MOVE** | BenchmarkDotNet micro-benchmarks. |
| `deploy/` | **MOVE** | Docker-packaged .NET deployable (`deploy/Mesh/Benzene.Mesh.Host`). |
| `tools/` | **MOVE** | `tools/Benzene.Descriptor` (.NET tool). |
| `Benzene.sln` | **MOVE** | Main library solution. |
| `Benzene.Examples.sln` | **MOVE** | Examples solution. |
| `Directory.Build.props` | **MOVE** | .NET build props (shared by `src`/`test`). |
| `version.txt` | **MOVE** | .NET package version. |
| `CHANGELOG.md` | **MOVE** | .NET package changelog. |
| `VERSIONING.md` | **MOVE** | .NET package/versioning policy. |
| `website/` | **STAY** | The multi-language site generator (evolved in Phase 2). |
| `blog/` | **STAY** | Marketing narrative — language-neutral messaging (introducing/lock-in/honest-abstraction). |
| `work/` | **SPLIT** | `repo-split-plan.md`/`repo-split-manifest.md` **STAY**; the .NET roadmaps/designs (the bulk) **MOVE**. See "work/ split" below. |
| `README.md` | **BOTH** | `benzene`: rewritten as cross-language landing. `benzene-dotnet`: a .NET-port README (seeded from today's). |
| `AGENTS.md` / `CLAUDE.md` | **BOTH** | Each repo gets its own (spec+website vs .NET port). |
| `LICENSE` | **BOTH** | Same license copied to each repo. |
| `CODE_OF_CONDUCT.md`, `CONTRIBUTING.md`, `SECURITY.md` | **BOTH** | Standard governance; each repo needs its own (CONTRIBUTING diverges: .NET build vs website/spec). |
| `DOCUMENTATION_WRITER_SETUP.md` | **MOVE** | .NET-only today; a future cross-language version will have a crossover, adjusted then. |
| `.gitignore` | **BOTH** | Each repo gets a tailored copy. |
| `.claude/` | **SPLIT** | Best-endeavors: .NET-specific agents/skills **MOVE**, website/spec/marketing ones **STAY**. Duplicate where unsure; these need a review in the new structure regardless — sorted in the cleanup pass, not up front. |
| `.github/` | **SPLIT** | Workflows split per the table below; issue templates copied to **BOTH**. |

## `docs/` — the taxonomy split

**STAY in `benzene` (cross-language spec definition):** everything under `docs/specification/`.

| Path | Disposition |
|------|-------------|
| `docs/specification/README.md` | **STAY** |
| `docs/specification/design-principles.md` | **STAY** |
| `docs/specification/core-concepts.md` | **STAY** |
| `docs/specification/wire-contracts.md` | **STAY** |
| `docs/specification/transport-bindings.md` | **STAY** |
| `docs/specification/mesh.md` | **STAY** |
| `docs/specification/cloud-service-profile.md` | **STAY** |
| `docs/specification/versioning.md` | **STAY** |
| `docs/specification/porting-guide.md` | **STAY** |
| `docs/specification/conformance/README.md` | **STAY** (canonical) |
| `docs/specification/conformance/*.json` (8 fixtures) | **STAY** (canonical) — vendored copy taken into `benzene-dotnet` |

**MOVE to `benzene-dotnet` (.NET how-to / reference):** all other `docs/`:

- All `docs/getting-started-*.md` (aws, azure, cloudflare, grpc, kafka, rabbitmq, templates, worker) + `docs/getting-started.md` — **MOVE**
- `docs/spec.md` — **MOVE** (how to *use* the spec topic in .NET; distinct from the `specification/**` definition, per decision 5)
- `docs/hosting.md`, `docs/middleware.md`, `docs/common-middleware.md`, `docs/message-handlers.md`, `docs/message-result.md`, `docs/clients.md`, `docs/client-sdks.md` — **MOVE**
- `docs/asp-net-core.md`, `docs/azure-functions.md`, `docs/aws-iam-permissions.md`, `docs/terraform.md` — **MOVE**
- `docs/caching.md`, `docs/resilience.md`, `docs/rate-limiting.md`, `docs/correlation-ids.md`, `docs/sampling-strategies.md` — **MOVE**
- `docs/data-annotations.md`, `docs/fluent-validation.md`, `docs/payload-testing.md`, `docs/testing-benzene.md` — **MOVE**
- `docs/health-checks.md`, `docs/kubernetes-health-checks.md`, `docs/monitoring.md`, `docs/diagnosing-failures.md` — **MOVE**
- `docs/mesh-ui.md`, `docs/mesh-usage-feed.md`, `docs/capability-matrix.md`, `docs/deprecations.md`, `docs/privacy-and-data-handling.md` — **MOVE**
- `docs/spec-ui.md` — **MOVE**
- `docs/reference/**` (attributes, configuration, middleware, packages, results) — **MOVE**
- `docs/cookbooks/**` — **MOVE**
- `docs/plans/**` (10 internal .NET roadmap docs) — **MOVE**
- `docs/DOCUMENTATION_QUICK_REFERENCE.md` — **MOVE** (contributor cheat-sheet for .NET docs)
- `docs/index.md` — **SPLIT**: the .NET nav subtree seeds `benzene-dotnet`'s `docs/index.md`; the
  cross-language headline + the **Specification (Draft)** section stays and becomes `benzene`'s
  cross-language index.

## `.github/workflows/` split

| Workflow | Disposition |
|----------|-------------|
| `build-benzene.yml` | **MOVE** |
| `deploy-benzene.yml` | **MOVE** |
| `build-templates.yml`, `deploy-templates.yml` | **MOVE** |
| `build-mesh-host.yml`, `deploy-mesh-host.yml` | **MOVE** |
| `deploy-asp-example.yml`, `deploy-aws-example.yml`, `deploy-aws-mesh-example.yml`, `destroy-aws-mesh-example.yml` | **MOVE** |
| `deploy-azure-functions-mesh-example.yml`, `deploy-azure-mesh-example.yml` | **MOVE** |
| `deploy-eks-mesh-example.yml`, `deploy-k8s-mesh-example.yml` | **MOVE** |
| `deploy-google-cloud-mesh-example.yml`, `deploy-google-example.yml`, `deploy-google-function-example.yml` | **MOVE** |
| `main_benzene-example.yml`, `smoke-mesh-compose.yml` | **MOVE** |
| `deploy-website.yml` | **STAY** — reworked in Phase 3 for multi-checkout. |
| `promote-website.yml` | **STAY** |
| *(new)* conformance-drift-check | **NEW in `benzene-dotnet`** — fetch canonical fixtures from `benzene`, diff, fail on drift. |
| *(new)* cross-repo site trigger | **NEW in `benzene-dotnet`** — on push to `main`, trigger `benzene`'s website deploy. |

## Conformance fixtures (decision 2, option 1)

- Canonical: `benzene` `docs/specification/conformance/*.json` (**STAY**).
- `benzene-dotnet` gets a **vendored snapshot** at `test/conformance-fixtures/*.json` + a
  `SPEC_VERSION` marker file.
- `test/Benzene.Conformance.Test/ConformanceFixtures.cs` is re-pointed from
  `docs/specification/conformance/` to the vendored dir (the one required code change in the move).
- Drift-check workflow (above) keeps the snapshot honest.

## `work/` split (detail)

- **STAY:** `repo-split-plan.md`, `repo-split-manifest.md` (this migration lives in `benzene`).
- **MOVE (default):** everything else in `work/` is .NET roadmap/design/review material
  (`aws-roadmap-1.0.md`, `azure-roadmap-1.0.md`, `1.0-*`, `client-health-checks-*`,
  `settlement-contract-1.0.md`, the `arch-review/` and `bughunt/` dirs, etc.). Bulk-move it.
- **Duplicate where neutral:** language-neutral strategy notes (`benzene-vision.md`,
  `benzene-naming-principle.md`, `benzene-production-provenance.md`, the website/marketing plans)
  arguably belong with `benzene`/`website`. Best-endeavors — copy to both if unsure. `work/` is
  largely standalone/old and is due a review of its own; that review, not this split, sorts it out.

## Resolved defaults (best-endeavors; cleanup pass follows)

Per the maintainer, these are settled with reasonable defaults rather than held as blockers:

1. `DOCUMENTATION_WRITER_SETUP.md` → **MOVE** (.NET-only now; cross-language crossover handled later).
2. `.claude/` agents/skills → **best-endeavors split**, duplicate where unsure; reviewed in the
   post-split cleanup (they need re-review under the new structure anyway).
3. `work/` notes → **bulk-move the .NET material**, duplicate neutral notes; `work/` is due its own
   review regardless.
4. Governance files → **copy to both**; let them diverge naturally over time (CONTRIBUTING will).

None of these block Phase 1. The cleanup/review pass after the split reconciles any duplication or
misplacement.
