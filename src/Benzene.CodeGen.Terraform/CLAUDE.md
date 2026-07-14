# Benzene.CodeGen.Terraform

## What this package does
Generates Terraform (`.tf`) configuration for AWS Lambda functions hosting Benzene services, so
the infrastructure that delivers events to a service is derived from the service's code instead
of hand-maintained. See `docs/terraform.md`.

## Key types/interfaces
- `TerraformLambdaBuilder : ICodeBuilder<TerraformLambdaSettings>` — the entry point: generates
  `aws_lambda_function` + `aws_iam_role`, and composes the event-source builders below when
  their settings are present (`TopicsMap` → SNS, `EventBridge` → EventBridge).
- `TerraformLambdaEventBusPermissionsBuilder` — SNS-side wiring: `aws_lambda_permission` +
  `aws_sns_topic_subscription` with a `filter_policy` over the Benzene message topics.
- `TerraformEventBridgeRuleBuilder : ICodeBuilder<TerraformEventBridgeRuleSettings>` —
  EventBridge-side wiring: one `aws_cloudwatch_event_rule` whose `event_pattern` matches
  `detail-type` against the service's message topics verbatim (the same contract
  `Benzene.Aws.Lambda.EventBridge` routes on), plus `aws_cloudwatch_event_target` and
  `aws_lambda_permission` (file `aws_lambda_permission_eventbridge.tf` — deliberately distinct
  from the SNS builder's `aws_lambda_permission.tf` so both can coexist). Optional
  `EventBusName` (default bus when null) and `Sources` filter.
- `TerraformEventBridgeRuleBuilderExtensions` — `BuildCodeFiles(settings, ...)` overloads that
  discover the topics from `[Message]` handlers via `ReflectionMessageHandlersFinder`
  (`IMessageHandlerDefinition[]`, `params Type[]`, or `params Assembly[]`), de-duplicated on
  `Topic.Id` (handler versions never reach the wire) and ordinal-sorted for diff-stable output.
- `TerraformDirectoryMerger` — merges generated files into an existing Terraform directory.

## Important conventions
- Builders emit `ICodeFile[]` (one file per resource type) via `LineWriter(2)` — two-space
  indent, `StartIndent()` blocks.
- Terraform resource labels are `NameFormatter.UnderScoreCase(lambdaName)`; generated resources
  reference the co-generated Lambda as `aws_lambda_function.<label>` — the builders assume they
  run alongside `TerraformLambdaBuilder`.
- An EventBridge rule with no topics throws (`ArgumentException`) rather than emitting a
  match-nothing pattern.

## Dependencies on other Benzene packages
- **Benzene.CodeGen.Core** — `ICodeBuilder`, `CodeFile`, `LineWriter`
- **Benzene.Core.MessageHandlers** — `ReflectionMessageHandlersFinder` for `[Message]` topic
  discovery
