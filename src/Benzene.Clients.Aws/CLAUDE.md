# Benzene.Clients.Aws (meta-package)

## What this package is now
A **thin meta-package** with no code of its own. It exists only to reference all five per-transport
AWS client packages, so a consumer who genuinely wants every AWS outbound client can keep a single
`Benzene.Clients.Aws` reference. It carries **no** `PackageReference` to any AWS SDK directly — each
SDK comes from the transport package that needs it.

## Why it was split (2026-07-18, release plan Tier 2.1)
The outbound AWS clients used to all live in this one package, so referencing it to publish to SNS
dragged in the SQS, Lambda, EventBridge, and Step Functions SDKs too. They are now segregated by
transport — mirroring how *ingress* is split (`Benzene.Aws.Lambda.Sqs`/`.Sns`/…) — so each package
pins only the one AWS SDK it uses:

| Package | Contains | AWS SDK pinned |
|---|---|---|
| `Benzene.Clients.Aws.Sqs` | SQS send client, converters, `SqsHealthCheck`, `AddSqsHealthCheck` | `AWSSDK.SQS` |
| `Benzene.Clients.Aws.Sns` | SNS publish client, converters | `AWSSDK.SimpleNotificationService` |
| `Benzene.Clients.Aws.EventBridge` | EventBridge put-events client | `AWSSDK.EventBridge` |
| `Benzene.Clients.Aws.Lambda` | Lambda invoke client, `AwsLambdaHealthCheck`, `AddLambdaHealthCheck` | `AWSSDK.Lambda` |
| `Benzene.Clients.Aws.StepFunctions` | StartExecution client, `StepFunctionsHealthCheck`, `AddStepFunctionHealthCheck` | `AWSSDK.StepFunctions` |

## Guidance
- **New code** should reference the specific transport package(s) it needs, not this meta-package.
- The **namespaces are unchanged** (`Benzene.Clients.Aws.Sqs`, `.Sns`, `.EventBridge`, `.Lambda`,
  `.StepFunctions`) — moving to a per-transport package reference is a `.csproj` change only; no
  `using` or type reference in consumer code changes.
- **One thing did move namespace:** the health-check registration extensions. They were on a single
  root-namespace `Benzene.Clients.Aws.Extensions` class and are now on each transport package's own
  `Extensions` class, so `AddSqsHealthCheck`/`AddLambdaHealthCheck`/`AddStepFunctionHealthCheck` now
  live in `Benzene.Clients.Aws.Sqs`/`.Lambda`/`.StepFunctions` respectively. Add the matching `using`.
- Do not add code or SDK `PackageReference`s here — put them in the relevant transport package.

## See also
Each transport package has its own `CLAUDE.md` with the per-transport conventions (header/attribute
forwarding, the `IBenzeneResult<Void>` outbound-cast rule, the deferred `.UseAwsLambda` outbound
overload, etc.).
