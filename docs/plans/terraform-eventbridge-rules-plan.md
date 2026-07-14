# Terraform EventBridge Rule Generation Plan

## Why

`docs/plans/eventbridge-plan.md` deferred one piece of the EventBridge integration as future work:
generating the EventBridge-side infrastructure from the handlers themselves. A Benzene service
already declares exactly which events it consumes — every `[Message("<detail-type>")]` handler is
a routing claim — so the `aws_cloudwatch_event_rule` / target / permission wiring that delivers
those events to the Lambda is derivable mechanically. Hand-written rules drift from the code;
generated rules cannot.

The repo already has the scaffolding for this: `src/Benzene.CodeGen.Terraform` generates the
Lambda function, IAM role, and (for SNS consumers) `aws_lambda_permission` +
`aws_sns_topic_subscription` resources from a settings object, and
`ReflectionMessageHandlersFinder` (`src/Benzene.Core.MessageHandlers`) already extracts
`IMessageHandlerDefinition[]` (topic id + version + types) from handler assemblies — it is what
the OpenAPI/Markdown/client generators are built on. This plan adds the EventBridge builder next
to the SNS one, following the same shapes.

## Scope

New builder + settings in `src/Benzene.CodeGen.Terraform`, integration into
`TerraformLambdaBuilder`, topic discovery from `[Message]` attributes, tests, and docs.
**No new projects, no new NuGet dependencies** (only an explicit `ProjectReference` from
`Benzene.CodeGen.Terraform` to `Benzene.Core.MessageHandlers`, which is already a transitive
dependency via `Benzene.CodeGen.Core`).

Out of scope: generating the event bus itself, cross-account rules, input transformers,
dead-letter configs on the target, and CLI wiring (`Benzene.CodeGen.Cli` is driven by a live
Lambda's spec endpoint, not local assemblies — hooking this builder into it is a separate piece
of work).

## Verified facts this plan relies on

- The inbound adapter (`src/Benzene.Aws.Lambda.EventBridge`) matches topics against
  `detail-type` **verbatim** (decision E1), and `source` is metadata, not part of the topic. So a
  rule's `event_pattern` must filter on `detail-type`; a `source` filter is an optional extra.
- Handler versions (`[Message("topic", "2.0")]`) do not appear on the wire — all versions of a
  topic share one `detail-type` — so discovery must de-duplicate on `Topic.Id` and ignore
  `Version`.
- `ICodeBuilder<T>.BuildCodeFiles(T)` → `ICodeFile[]` (name + lines) is the codegen contract;
  builders write lines via `LineWriter(2)` with `StartIndent()` blocks
  (`src/Benzene.CodeGen.Core/Writers/LineWriter.cs`).
- `TerraformLambdaBuilder` composes sub-builders by appending their `ICodeFile`s when the
  corresponding settings are present (`TopicsMap` → SNS builder), and resource names are
  underscore-cased via `NameFormatter.UnderScoreCase`.
- `ToFilesDictionary()` throws on duplicate file names, and the SNS builder already emits
  `aws_lambda_permission.tf` — the EventBridge permission file therefore needs a distinct name.
- `ReflectionMessageHandlersFinder` has `params Assembly[]` and `params Type[]` constructors and
  `FindDefinitions()` returns one definition per handler (so the same topic can appear multiple
  times across versions).

## Design decisions (final)

- **T1 — Pattern matches `detail-type` verbatim.** `event_pattern` is emitted as
  `jsonencode({ "detail-type" = [<topics>] })`, mirroring exactly what
  `EventBridgeMessageTopicGetter` matches on. When `Sources` is set, a `source = [<sources>]`
  clause is added (narrows delivery to trusted publishers; the inbound adapter treats `source` as
  metadata either way).
- **T2 — One rule per Lambda, all topics in one pattern.** Mirrors the SNS builder's
  one-subscription-with-`filter_policy` shape: one `aws_cloudwatch_event_rule`, one
  `aws_cloudwatch_event_target`, one `aws_lambda_permission` per consuming Lambda. Adding a
  handler changes the pattern in place — a minimal Terraform diff — and keeps well clear of the
  rules-per-bus quota. Per-topic rules (for per-topic CloudWatch metrics/disable) are future
  work, not a setting on day one.
- **T3 — File layout follows the SNS builder** (one file per resource type):
  `aws_cloudwatch_event_rule.tf`, `aws_cloudwatch_event_target.tf`, and
  `aws_lambda_permission_eventbridge.tf` (distinct from the SNS builder's
  `aws_lambda_permission.tf` so both can be generated for the same Lambda).
- **T4 — References assume the co-generated Lambda.** `function_name` / `arn` reference
  `aws_lambda_function.<underscore_name>`, exactly like the SNS builder — the builder is designed
  to run alongside `TerraformLambdaBuilder`, not against arbitrary externally-defined Lambdas.
- **T5 — `EventBusName` is an optional literal.** When set, `event_bus_name = "<name>"` is
  emitted on both rule and target; when null the attribute is omitted (default bus). No support
  for Terraform expressions as bus references — pass a literal or edit the output.
- **T6 — Topic discovery is an overload, not magic.** The builder itself takes explicit
  `Topics`. Extension overloads fill `Topics` from `IMessageHandlerDefinition[]`, `params Type[]`,
  or `params Assembly[]` via `ReflectionMessageHandlersFinder`, de-duplicated on `Topic.Id` and
  ordinal-sorted (stable output → stable diffs).
- **T7 — Empty topics fail fast.** A rule with an empty `detail-type` array matches nothing and
  is almost certainly a mis-scan; `BuildCodeFiles` throws `ArgumentException` instead of emitting
  it.
- **T8 — `TerraformLambdaSettings` grows an `EventBridge` property.** When non-null,
  `TerraformLambdaBuilder` appends the EventBridge files, defaulting
  `EventBridge.LambdaName` to `settings.Name` — same composition style as `TopicsMap`.

## Generated output (shape)

```hcl
resource "aws_cloudwatch_event_rule" "my_service_func_event_rule" {
  name = "my-service-func-event-rule"
  event_bus_name = "orders-bus"            # only when EventBusName is set
  event_pattern = jsonencode({
    "detail-type" = ["order.created", "order.updated"]
    "source" = ["com.example.orders"]      # only when Sources is set
  })
}

resource "aws_cloudwatch_event_target" "my_service_func_event_target" {
  rule = aws_cloudwatch_event_rule.my_service_func_event_rule.name
  event_bus_name = "orders-bus"            # only when EventBusName is set
  arn = aws_lambda_function.my_service_func.arn
}

resource "aws_lambda_permission" "eventbridge_invoke_my_service_func" {
  action = "lambda:InvokeFunction"
  function_name = aws_lambda_function.my_service_func.function_name
  principal = "events.amazonaws.com"
  statement_id = "AllowEventBridgeInvoke"
  source_arn = aws_cloudwatch_event_rule.my_service_func_event_rule.arn
}
```

## Work items

1. `src/Benzene.CodeGen.Terraform/TerraformEventBridgeRuleSettings.cs` — `LambdaName`,
   `EventBusName` (optional), `Sources` (optional), `Topics`.
2. `src/Benzene.CodeGen.Terraform/TerraformEventBridgeRuleBuilder.cs` —
   `ICodeBuilder<TerraformEventBridgeRuleSettings>` emitting the three files above.
3. `src/Benzene.CodeGen.Terraform/TerraformEventBridgeRuleBuilderExtensions.cs` — the
   `IMessageHandlerDefinition[]` / `Type[]` / `Assembly[]` discovery overloads (T6).
4. `TerraformLambdaSettings.EventBridge` + composition in `TerraformLambdaBuilder` (T8).
5. Explicit `ProjectReference` to `Benzene.Core.MessageHandlers` in
   `Benzene.CodeGen.Terraform.csproj`.
6. Tests in `test/Benzene.Core.Test/Autogen/CodeGen/Terraform/TerraformEventBridgeRuleBuilderTest.cs`:
   full-content line assertions for all three files; bus-name/sources variants; discovery
   de-dup across handler versions (`ExampleMessageHandler` + `ExampleMessageHandlerV2` share a
   topic) and ordering; empty-topics throw; `TerraformLambdaBuilder` integration.
7. Docs: EventBridge section in `docs/terraform.md`; new
   `src/Benzene.CodeGen.Terraform/CLAUDE.md`; cross-link from the EventBridge package CLAUDE.md;
   strike the "future work" deferral note in `docs/plans/eventbridge-plan.md`.
