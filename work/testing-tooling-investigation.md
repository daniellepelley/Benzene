# Testing-Tooling Investigation Plan

**Status: investigation plan, not a commitment.** Two popular .NET testing libraries look like they
could improve Benzene's *internal* test infrastructure. Neither is a shipped package — there is no
public-API impact — so this is a scoped spike-then-decide, deliberately sequenced *after* the Polly
and RabbitMQ work (RabbitMQ's live integration test is a natural first Testcontainers candidate).

## Candidate 1 — Testcontainers for .NET

**What it is:** [Testcontainers for .NET](https://dotnet.testcontainers.org/) spins up throwaway
Docker containers per test run, with first-party modules for common services.

**Current state it would replace:** Benzene's integration tests hand-roll Docker via
`Ductus.FluentDocker` and a custom `DockerComposeFixture`, with per-service fixtures
(`test/Benzene.Integration.Test/Fixtures/` — Event Hub, Service Bus; `test/Benzene.Aws.Tests/Fixtures/`
— LocalStack, SQS) driven by checked-in `docker-compose.yaml` files. This also forces the CI
`docker-compose`-CLI shim (the "ensure docker-compose is resolvable" step in the workflows, because
FluentDocker shells out to the v1 binary name).

**Questions the spike must answer:**
- Does Testcontainers have (or can we cleanly wrap) the specific emulators Benzene needs — LocalStack,
  the Azure Service Bus emulator, the Event Hubs emulator, RabbitMQ, Kafka, Redis? (Modules exist for
  several; the Azure emulators are the risk.)
- Does it remove the `docker-compose` CLI shim requirement in CI (it talks to the Docker daemon
  directly)?
- CI cost: container start/stop time and reliability vs the current compose fixtures; Ryuk
  resource-reaper behavior on the CI runner.
- Migration cost per fixture, and whether both approaches can coexist during a gradual migration.

**Spike:** migrate **one** fixture end-to-end — the strongest candidate is a fresh one rather than a
rewrite, i.e. stand up the **RabbitMQ live integration test** (from `docs/plans/rabbitmq-plan.md`
Phase 3) on Testcontainers, and separately convert one existing fixture (e.g. `SqsFixture`/LocalStack)
to compare against the current FluentDocker approach. Measure setup time, CI reliability, and code
delta. Then decide whether to migrate the rest.

## Candidate 2 — Verify (snapshot testing)

**What it is:** [Verify](https://github.com/VerifyTests/Verify) is the .NET-standard snapshot/approval
testing library — assert that produced output matches a stored snapshot, with an approve-on-diff
workflow.

**Current state it would replace:** Benzene's codegen and schema tests compare generated output
against many hand-maintained golden files — `SpecTest` (OpenAPI/AsyncAPI/benzene spec docs) and the
large `test/Benzene.Core.Test/Autogen/**/Examples/*` corpus (Terraform, Markdown, client-SDK,
schema JSON/YAML). These are exactly the "assert output shape" cases Verify is built for.

**Questions the spike must answer:**
- Which suites benefit most (codegen: Terraform/Markdown/client-SDK; `Schema.OpenApi` golden
  JSON/YAML)? Estimate the golden-file boilerplate Verify removes.
- CI behavior: Verify must **fail on mismatch in CI** (no interactive prompt, no auto-approve), and
  the local approve workflow needs a diff tool — confirm both are clean on this repo's runners.
- Interaction with the existing xUnit setup and the deterministic-output requirements (ordering,
  timestamps, line endings) some of these golden files already wrestle with.

**Spike:** convert **one** golden-file suite (e.g. one `Benzene.CodeGen.Markdown` or
`Schema.OpenApi` test) to Verify, wire the CI-fail-on-diff mode, and compare readability/maintenance
against the current manual `Assert.Equal(expected, actual)` + checked-in file approach.

## Explicitly out of scope

- Shipping either as a Benzene package — both are dev/test dependencies only.
- A big-bang migration — the deliverable of this investigation is *two spikes + a recommendation*,
  not a wholesale conversion.
- Property-based testing, mutation testing, architecture-testing libraries, etc. — not part of this
  investigation.

## Deliverable

A short findings note (append to this doc) after the two spikes: for each library, a
migrate / don't-migrate recommendation with the measured evidence (CI time, reliability, code delta,
maintenance burden), and if "migrate", a follow-up plan for the rollout.
