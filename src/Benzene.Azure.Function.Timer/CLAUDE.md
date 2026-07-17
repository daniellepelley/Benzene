# Benzene.Azure.Function.Timer

## What this package does
Azure Functions `TimerTrigger` adapter (isolated worker): delivers scheduled ticks into a Benzene
middleware pipeline, so scheduled jobs get the same pipeline composition
(correlation/metrics/exception handling) as every other entry point — and, via a preset topic, can
invoke the *same message handlers* as any messaging transport.

## Zero dependencies — deliberately
References only `Benzene.Azure.Function.Core` + `Benzene.Core.MessageHandlers`. The consumer's
Function App project references `Microsoft.Azure.Functions.Worker.Extensions.Timer` itself for the
attribute; `TimerTriggerInfo`/`TimerScheduleStatus` property names match the isolated worker's
`TimerInfo` JSON, so the trigger parameter can be bound directly as Benzene's type. Do not add SDK
packages here without asking first (repo NuGet policy).

## Two consumption modes
1. **Direct** — `UseTick(...)` terminal sugar (context and info overloads), for scheduled work
   that doesn't need routing.
2. **Message-handler dispatch** — `UsePresetTopic("nightly-cleanup").UseMessageHandlers()`: the
   tick routes to the handler declaring that topic, making a scheduled job just another message
   handler (testable/portable like any other). The tick's body is the serialized
   `TimerTriggerInfo` (via `TimerMessageBodyGetter`, ctor-injected `JsonSerializer`), so a handler
   request type mirroring its properties receives the schedule info, and an empty request type
   binds cleanly.

## Naming caution
The builder extension is **`UseTimerTrigger`** — NOT `UseTimer`, which already exists in
`Benzene.Core.Middleware` as the timing middleware. Keep it that way.

## Key types
- `TimerTriggerInfo` / `TimerScheduleStatus` — dependency-free models (`IsPastDue`,
  `Last`/`Next`/`LastUpdated`).
- `TimerContext : IHasMessageResult` — diagnostics-only result; a tick has no caller to answer.
- `TimerApplication` — `EntryPointMiddlewareApplication<TimerTriggerInfo>`, transport tag
  `"timer"`, one DI scope per tick.
- `UseTimerTrigger(action)` (both builders, no-op off-Azure), `AddAzureTimer()`,
  `TimerRegistrations`, `UseTick(...)`, `HandleTimer(TimerTriggerInfo)` / `HandleTimer()`.

## Failure handling
None in-package: a pipeline exception propagates to the host, which logs the failed invocation.
Note the platform reality: the timer trigger does **not** retry a failed tick — the next
occurrence just runs on schedule — so a job needing at-least-once semantics should enqueue work
(queue/Service Bus) rather than doing it inline in the tick.

## Tests
- `test/Benzene.Core.Test/Azure/TimerPipelineTest.cs` — tick delivery with schedule info,
  preset-topic dispatch to a real message handler, exception propagation, platform-neutral no-op.
