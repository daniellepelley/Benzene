# Repo split — Phase 1 file-move manifest

Companion to **[repo-split-plan.md](repo-split-plan.md)**. This is the exact, reviewable split of
what goes to **`benzene-dotnet`** (MOVE) vs stays in **`benzene`** (STAY), as of the current tree.
Nothing is created or deleted yet — this is the artifact to sign off before Phase 1.

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
| `DOCUMENTATION_WRITER_SETUP.md` | **MOVE** | Doc-writer setup is .NET-docs tooling. (Confirm at Phase 1.) |
| `.gitignore` | **BOTH** | Each repo gets a tailored copy. |
| `.claude/` | **SPLIT** | Agents/skills that are .NET-specific **MOVE**; website/spec/marketing ones **STAY**. Audit per-file at Phase 1. |
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
- **MOVE:** everything else in `work/` is .NET roadmap/design/review material
  (`aws-roadmap-1.0.md`, `azure-roadmap-1.0.md`, `1.0-*`, `client-health-checks-*`,
  `settlement-contract-1.0.md`, the `arch-review/` and `bughunt/` dirs, etc.).
- **Reconsider case-by-case:** language-neutral strategy notes (`benzene-vision.md`,
  `benzene-naming-principle.md`, `benzene-production-provenance.md`, the website/marketing plans)
  arguably belong with `benzene`/`website`. Flag these individually at Phase 1 rather than
  bulk-moving.

## Open items to confirm at Phase 1 sign-off

1. `DOCUMENTATION_WRITER_SETUP.md` — .NET-only, or cross-repo tooling? (Assumed MOVE.)
2. `.claude/` agents/skills — per-file audit: which are .NET-port vs spec/website/marketing.
3. `work/` language-neutral notes — individual stay/move calls (list above).
4. Which governance files genuinely diverge vs are byte-identical copies (CONTRIBUTING will diverge).
