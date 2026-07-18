# Benzene.Clients.Aws.StepFunctions

## What this package does
Outbound AWS Step Functions client for a Benzene app: start a state machine execution, plus a Step
Functions health check. Pins **only** `AWSSDK.StepFunctions`.

## Key types
- `IStepFunctionsClient` / `StepFunctionsClient` — `StartExecutionAsync<TMessage, TResponse>` starts
  an execution with the serialized message as input.
- `StepFunctionsClientFactory` — builds a client for a given state-machine ARN.
- `StepFunctionsHealthCheck` — starts an execution as a liveness probe; reports
  `HealthCheckDependency` (`Kind = "StateMachine"`, `Name` = ARN).
- `Extensions` — **`AddStepFunctionHealthCheck`**.

## Scope / honesty (release plan Tier 2.5 — decision pending)
`StartExecutionAsync` is **fire-and-forget**: it starts the execution and does not thread the
execution ARN back to the caller, so there is no built-in way to await or correlate the result. That
is the honest 1.0 scope. Task-token callbacks / awaiting completion are a candidate post-1.0
deepening — do not document a request/reply capability this package does not have.

## Dependencies
`AWSSDK.StepFunctions`; Benzene `Clients`, `HealthChecks.Core`, `Results`.
