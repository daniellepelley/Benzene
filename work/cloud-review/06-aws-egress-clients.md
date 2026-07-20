## AWS egress clients

Scope note: `src/Benzene.Clients.Aws` is a code-free meta-package (only CLAUDE.md). AWS facts confirmed via search (docs.aws.amazon.com blocks the sandbox proxy directly).

---

### Lambda (`src/Benzene.Clients.Aws.Lambda`)

**[DIVERGENCE] `FunctionError` is never inspected — a failed function reads as a garbled/opaque error** (Severity: High)
- **Benzene today:** `AwsLambdaClient.SendMessageAsync` (`:42-55`) invokes, then for `RequestResponse` unconditionally deserializes `lambdaResponse.Payload` into a `BenzeneMessageClientResponse` and maps via `AsBenzeneResult`. `InvokeResponse.FunctionError` is never read.
- **AWS intent:** A `RequestResponse` invoke where the handler throws returns **HTTP 200** with `FunctionError` set to `"Handled"`/`"Unhandled"`, and `Payload` is an error object (`{errorMessage, errorType, stackTrace}`) — not normal output. "The status code doesn't reflect function errors"; `FunctionError` is the signal.
- **Impact:** On a function failure the error JSON has no `statusCode` field, so `AsBenzeneResult` produces an `UnexpectedError`/"status code not mapped" result (or mis-deserializes). The real failure is lost; a function error is indistinguishable from a transport fault. Classic Lambda "200-but-failed" trap.
- **Recommendation:** Check `lambdaResponse.FunctionError` before treating payload as success; surface it as a distinct failure carrying `errorType`/`errorMessage`.

**[WRONG-APPROACH] The `UseAwsLambda()` / `LambdaContextConverter` path does a synchronous invoke but discards the response and ignores errors** (Severity: Medium)
- **Benzene today:** `LambdaContextConverter.CreateRequestAsync` builds an `InvokeRequest` with **no `InvocationType`** (SDK default `RequestResponse`) and `MapResponseAsync` hardcodes `contextIn.Response = BenzeneResult.Accepted<Void>()` without reading the response.
- **AWS intent:** `RequestResponse` waits for the full execution (pays latency + duration) precisely so the caller can use the payload; `Event` is fire-and-forget (202, no payload).
- **Impact:** Pays for a synchronous invoke, throws the result away, reports `Accepted` regardless of outcome (incl. `FunctionError` or non-2xx). Fire-and-forget semantics on the most expensive invoke type, swallowing failures.
- **Recommendation:** Either set `InvocationType = Event` (true fire-and-forget) or map the captured `InvokeResponse` (status + `FunctionError`). At minimum document.

**[DIVERGENCE] Invocation type inferred from a fragile type-name compare; no `DryRun`** (Severity: Medium)
- `AwsLambdaBenzeneMessageClient.SendMessageAsync` picks invoke type with `typeof(TResponse).Name == "Void" ? Event : RequestResponse`. Matches **any** type named `Void` in any namespace (latent footgun); no `DryRun`; the `Event` path's 202/async-destination/retry behavior is neither verified nor surfaced. Compare against the actual `Benzene.Abstractions.Results.Void` type; consider an explicit invoke-type option.

**[MISSING] Payload size limits not guarded/documented** (Severity: Low)
- `RequestResponse` max 6 MB; async `Event` far smaller (256 KB). Oversize → SDK throw → generic `ServiceUnavailable`. Worth a doc note, especially the async ceiling.

**Verdict — Lambda:** Happy path works, but the failure path is materially wrong: `FunctionError` never inspected (High), and `UseAwsLambda()` silently discards response and error. Fix `FunctionError` first.

---

### StepFunctions (`src/Benzene.Clients.Aws.StepFunctions`)

**[MISSING] No idempotency token — `StartExecutionRequest.Name` is never set** (Severity: Medium)
- Sets only `StateMachineArn` + `Input`; `Name` left null (AWS auto-generates a UUID each call). The execution `Name` is StartExecution's idempotency mechanism (Standard workflows: same `Name` within the 90-day window is idempotent → `ExecutionAlreadyExists`). With no `Name`, any at-least-once retry (incl. this repo's outbound `RetryMiddleware`) after a successful-but-lost response starts a **duplicate execution**. Allow a caller-supplied/correlation-derived name.

**[DIVERGENCE] `StartExecution` only; response discarded, no Express/`StartSyncExecution`, no result retrieval** (Severity: Low — honestly documented)
- Returns `Accepted` and discards `StartExecutionResponse` (drops `ExecutionArn` + `StartDate`). No way to await, correlate, or retrieve a result; no task-token callback. Fully owned in CLAUDE.md (Tier 2.5, "honest fire-and-forget for 1.0"). When picked up, thread `ExecutionArn` back and add a `StartSyncExecution` path for Express.

**[MISSING] Input 256 KB limit not guarded** (Severity: Low) — oversize → SDK throw → `ServiceUnavailable`. Minor.

**Verdict — StepFunctions:** Behaves as its CLAUDE.md claims (honest fire-and-forget start). The one undocumented gap is the absent execution-name idempotency token — flag for the retry story.

---

### EventBridge (`src/Benzene.Clients.Aws.EventBridge`)

**[Verified correct — NOT a gap] Per-entry partial failure IS handled**
- `EventBridgeResultMapper.Map` checks `response.FailedEntryCount > 0`, pulls the first entry with a non-empty `ErrorCode`, returns `ServiceUnavailable` with `"{ErrorCode}: {ErrorMessage}"`. Correctly matches AWS (PutEvents can return 200 with `FailedEntryCount > 0`). The one client that gets partial-failure right.

**[MISSING] No PutEvents batching — one API call per event** (Severity: Medium)
- Always builds a single-entry request. PutEvents accepts **up to 10 entries per call** (256 KB total). N events → N calls instead of ⌈N/10⌉ (up to 10× request count/cost). Offer a batch send (chunk into ≤10-entry requests).

**[DIVERGENCE] `Detail` may be emitted as non-object JSON** (Severity: Low)
- `BuildDetail` returns raw serialized message when there are no headers, and falls back to raw json if the parsed node isn't a `JsonObject`. `Detail` must be "a valid JSON object" — a scalar/array is rejected per-entry (now surfaced as `ServiceUnavailable`, but opaque). Wrap non-object payloads or validate up front.

**[MISSING] `Resources` / `Time` / `TraceHeader` entry fields not settable** (Severity: Low)
- Only `Source`, `DetailType`, `Detail`, `EventBusName` are set. `Resources` (rule matching/targeting) and `Time` have no story. Minor.

**Verdict — EventBridge:** Entry shape and partial-failure handling correct; the real gap is absent batched PutEvents (up to 10/call).
