# Benzene.CodeGen.LambdaTestTool

## What this package does
Generates per-topic test payload JSON files from a benzene spec (`EventServiceDocument`) — one
file per topic per transport — for firing at a locally running service, e.g. via the AWS Lambda
Test Tool or the `/benzene-message` HTTP endpoint. Part of the payload-testing story documented
in `docs/payload-testing.md`.

Formerly named `Benzene.CodeGen.MockLambdaTool` (the directory/package name was out of sync with
the `Benzene.CodeGen.LambdaTestTool` namespace; the rename fixed that).

## Key types
- `LambdaTestFilesBuilder : ICodeBuilder<EventServiceDocument>` — for every request topic, runs
  each `IExampleBuilder` and emits `<topic-with-dashes>-<transport>.json` files. The
  parameterless constructor uses `DefaultExampleBuilders.Create()`.
- `DefaultExampleBuilders` — the standard builder set:
  - `benzene-message` — the `{topic, headers, body}` envelope accepted by direct Lambda invoke
    and the `UseBenzeneMessage` HTTP endpoint (`Benzene.Http.BenzeneMessage`).
  - `sns` / `sqs` — full Lambda event JSON via `Benzene.Testing`'s `MessageBuilder` and the
    `AsSns()`/`AsSqs()` TestHelpers extensions.
  - `api-gateway` — `APIGatewayProxyRequest` JSON via `HttpBuilder`/`AsApiGatewayRequest()`
    (only emitted for topics with HTTP mappings).
  Payload bodies come from `Benzene.Schema.OpenApi.Examples.ExamplePayloadBuilder`, so they are
  the same deterministic, validation-aware examples the spec itself embeds.

## CLI
`Benzene.CodeGen.Cli`'s `lambda-test-tool` command wraps this package: fetches the benzene spec
from a Lambda (`--profile`/`--lambda-name`) or reads it from disk (`--file`), and writes the
files to `--directory`.

## Dependencies on other Benzene packages
- `Benzene.CodeGen.Core` — `IExampleBuilder`/`ExampleBuilder`/`HttpExampleBuilder`, file writing.
- `Benzene.Testing` + `Benzene.Aws.Lambda.{Sns,Sqs,ApiGateway}.TestHelpers` — the transport
  envelope builders. This is a codegen/tooling package, not a runtime dependency, so referencing
  test helpers is acceptable here.

## Tests
- `test/Benzene.Core.Test/Autogen/CodeGen/LambdaTestTool/LambdaTestToolBuilderTest.cs`
- `test/Benzene.Core.Test/Autogen/CodeGen/LambdaTestTool/LambdaTestToolCommandTest.cs` (drives
  the CLI command end-to-end from a spec file on disk)
