# Benzene.Clients.Aws.StepFunctions

## What this package does
Outbound AWS Step Functions client for a Benzene app: start a state machine execution, plus a Step
Functions health check. Pins **only** `AWSSDK.StepFunctions`.

## Key types
- `IStepFunctionsClient` / `StepFunctionsClient` — `StartExecutionAsync<TMessage, TResponse>` starts
  an execution with the serialized message as input. An overload
  `StartExecutionAsync<TMessage, TResponse>(message, executionName)` sets the execution's idempotency
  name (`StartExecutionRequest.Name`) from a caller-supplied stable token (e.g. a correlation id),
  sanitized to Step Functions' allowed name charset/length — so a retry after a lost response won't
  start a duplicate execution. An `ExecutionAlreadyExistsException` (the name was already used) is
  treated as an idempotent success (`Accepted`), not a failure. The no-name overload is unchanged
  (AWS generates a UUID name).
- `StepFunctionsClientFactory` — builds a client for a given state-machine ARN.
- `StepFunctionsHealthCheck` — verifies a state machine; reports `HealthCheckDependency`
  (`Kind = "StateMachine"`, `Name` = ARN). Default `HealthCheckMode.Reachability` is a
  **non-destructive** read-only `DescribeStateMachine` call (`Type = "StepFunctions"`);
  `HealthCheckMode.Active` starts a real execution (`Type = "StepFunctions.Active"`, side-effecting).
  See `HealthCheckMode` in `Benzene.HealthChecks.Core`. Failures are classified via
  `HealthCheckError.Classify` (§3.9): a permission error (403) is a **Warning**, not a failure; the SDK
  `ErrorCode`/`StatusCode` are surfaced in `Data`, never the exception message.
- `Extensions` — **`AddStepFunctionsClient(arn)`** (the DI-registration seam this package previously
  lacked: registers `IStepFunctionsClientFactory`/`IStepFunctionsClient` for a fixed ARN, resolving
  `IAmazonStepFunctions` from DI, and — unless `healthCheck: false` — **auto-wires** a non-destructive
  `DescribeStateMachine` check on the **dependency** category, dedup `"StepFunctions:{arn}"`, deep
  `healthcheck` layer only; see `IDependencyHealthCheck`) and **`AddStepFunctionHealthCheck`** (the
  explicit builder helper, unchanged).

## Scope / honesty (release plan Tier 2.5 — decided 2026-07-19: honest fire-and-forget for 1.0)
`StartExecutionAsync<TMessage, TResponse>` is **fire-and-forget**. On success it returns an empty
`BenzeneResult.Accepted<TResponse>()` and **discards the `StartExecutionResponse`** — the new
execution's ARN and start date are not threaded back, and `TResponse` never carries a value (Step
Functions runs the state machine asynchronously; there is no synchronous output to map). So there is
**no built-in way to await, poll, or correlate** the execution result, and **no task-token callback**
(`SendTaskSuccess`/`SendTaskFailure`) support. A failure to *start* returns `ServiceUnavailable`.

This is the deliberate, honest 1.0 scope — do not document a request/reply or workflow-tracking
capability this package does not have. For anything more (capture the `ExecutionArn`,
`DescribeExecution` polling, task-token callbacks, `.sync` integration), use the raw
`IAmazonStepFunctions` SDK directly in your handler (principle 1: Benzene never hides the SDK).
Deepening this into a first-class awaited/callback client is an explicit **post-1.0** item (release
plan Post-1.0 list: "Durable/orchestration depth — Step Functions task-token callbacks").

## Dependencies
`AWSSDK.StepFunctions`; Benzene `Clients`, `HealthChecks.Core`, `Results`.
