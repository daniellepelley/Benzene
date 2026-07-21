# Larger API-shape items — proposal for review (1.0)

These are the Tier-2 items that are **API/architecture decisions**, not mechanical fixes. Each is
laid out as: problem → current state (file-cited) → options → recommendation → breaking-ness + effort.
Nothing here is implemented yet — this doc is for a maintainer decision. Ordered by value-for-1.0.

---

## 1. Consolidate the three result abstractions

**Problem.** Three result types coexist and the ack/settlement path runs on the *legacy* one:
- `Benzene.Abstractions/Results/IBenzeneResult.cs` — `IBenzeneResult` / `IBenzeneResult<T>`: the
  public result a handler returns (`BenzeneResult.Ok(...)` etc.), six success statuses.
- `Benzene.Abstractions.MessageHandlers/IMessageHandlerResult.cs` — `IMessageHandlerResult<T>`:
  wraps a handler outcome with its topic/definition, used by the response pipeline.
- `Benzene.Abstractions.MessageHandlers/IMessageResult.cs` — legacy `IMessageResult`
  (`IsSuccessful`), what `IHasMessageResult`/the transport settlement code reads.

The settlement flip (just shipped) reads `context.MessageResult?.IsSuccessful` — the **legacy**
`IMessageResult`. New code standardizes on `IBenzeneResult`. Three overlapping abstractions on the
1.0 public surface is exactly the kind of thing to resolve *before* the freeze.

**Options.**
- **(1a) Keep all three, document the roles, no code change.** Zero risk, but ships the ambiguity.
- **(1b) `[Obsolete]` the legacy `IMessageResult`, route settlement through a single
  `IHasMessageResult` that exposes `IBenzeneResult`.** One canonical type; the transports read
  `IsSuccessful` off `IBenzeneResult.Status.IsSuccess()` instead of the legacy type. Source-compatible
  if `IMessageResult` stays (marked obsolete) for a release.
- **(1c) Remove `IMessageResult` outright.** Cleanest, but a hard breaking change for anyone who
  implemented it.

**Recommendation: (1b).** It removes the ambiguity from the *guidance* and the settlement path while
staying source-compatible for a deprecation window. Effort: medium (touch every transport's
result-setter + `IHasMessageResult`); risk: medium (settlement is load-bearing — the freshly-shipped
safe-default tests are the safety net). Breaking: only at the eventual `IMessageResult` removal.

---

## 2. SQS / DynamoDB adapter convergence (magic-string transport tags + `bool?` outcome)

**Problem.** Two adapters never converged onto the newer shape the rest use:
- Magic-string transport tags instead of `TransportNames`:
  `Benzene.Aws.Lambda.Sqs/SqsApplication.cs:67` → `setCurrentTransport.SetTransport("sqs")`,
  `Benzene.Aws.Lambda.DynamoDb/DynamoDbApplication.cs:53` → `SetTransport("dynamodb")`. `TransportNames`
  exists precisely so the runtime tag and the DI `ITransportInfo` registration can't drift — and SQS's
  own registration already uses `TransportNames.Sqs`, so one package sets the tag two ways.
- Bare `bool? IsSuccessful` on the context (`SqsMessageContext.cs`, `DynamoDbRecordContext.cs`) instead
  of `IHasMessageResult` + `MessageHandlerResultSetterBase<T>` (the one-liner the SNS/S3/Kafka/
  EventBridge/Kinesis adapters use). The `bool?` vs `IHasMessageResult` fork also forces outcomes to be
  read two ways (`!= true` vs `== false`).

**Options.**
- **(2a) Minimal:** just replace the two magic strings with `TransportNames.Sqs`/`.DynamoDb`. Tiny,
  safe, non-breaking. Leaves the `bool?` fork.
- **(2b) Full convergence:** move both onto `IHasMessageResult` + `MessageHandlerResultSetterBase<T>`,
  deleting the hand-rolled setters and the `bool?`. Internal shape change; the context types are public
  so the `IsSuccessful` property removal is technically breaking for anyone reading it directly.

**Recommendation: (2a) now, (2b) as a coordinated cleanup if we want it before freeze.** (2a) is a
free correctness/consistency win. (2b) is nice-to-have convergence debt, not a bug. Effort: (2a) tiny,
(2b) medium. This overlaps with item 1 (both touch the outcome abstraction) — do 1 and 2b together if
at all.

---

## 3. `SchemaCompatibilityComparer` gaps (contract-drift policy)

**Problem.** `Benzene.Schema.OpenApi/Compatibility/SchemaCompatibilityComparer.cs` passes some changes
as backward-compatible that arguably aren't: enum-value removal, nullable flips, and facet tightening
(`maxLength` shrink, `minimum` raise) currently don't flag as breaking. This is a **policy** call —
what counts as breaking depends on direction (a producer removing an enum value vs a consumer) and on
whether you gate reads, writes, or both.

**Options.**
- **(3a) Leave as-is**, document the known gaps as "advisory, not a gate."
- **(3b) Tighten per a written policy:** e.g. request-schema (inbound) tightening = breaking;
  response-schema (outbound) loosening = breaking; enum-value removal = breaking on the read side.
  Needs the policy written down first, then the comparer + tests follow it.

**Recommendation: write the policy (3b) but only if contract-drift is a gate anyone relies on.** If
it's currently advisory-only (surfaced in the mesh UI, not failing a build), (3a) + a doc note is fine
for 1.0 and this becomes a post-1.0 hardening. Effort: (3b) medium and needs your policy input first.
Breaking: none (it's analysis, not runtime).

---

## 4. Avro map support + unbounded-allocation cap

**Problem.** Two separate things in `Benzene.Avro/AvroSerializer.cs`:
- **Dictionary/map round-trip unsupported** — a handler payload with a `Dictionary<,>` doesn't
  round-trip through the Avro schema. Feature gap.
- **Unbounded length-prefix allocation on deserialize** — an untrusted `application/avro` body can
  declare a huge length prefix and drive an OOM before any data is read. Security/robustness bug.

**Options.**
- **(4a) Cap only (security):** add a configurable max-message/segment size to the Avro deserialize
  path (mirror `Benzene.SelfHost.Http`'s `MaxRequestBodyBytes` and the Queue-Storage size-guard
  pattern), reject oversize up front. Small, safe, no schema change.
- **(4b) Cap + map support:** (4a) plus a bidirectional map schema. The map support is a real schema
  change and more effort.

**Recommendation: (4a) now** — the OOM is the only genuinely *unsafe* part and it's self-contained.
**(4b) map support = post-1.0 feature** unless Avro maps are on a must-ship list. Effort: (4a) small,
(4b) medium. Breaking: none (both additive). *If you approve (4a), it's small enough that I can fold
it into the safe-hardening track rather than needing a design pass.*

---

## Suggested cut for 1.0

- **Do now (small, safe, mostly non-breaking):** 2a (transport-name constants), 4a (Avro OOM cap).
- **Do if you want the surface clean before freeze (medium, coordinated):** 1b + 2b together (result
  abstraction consolidation + SQS/DynamoDB convergence), with `[Obsolete]` deprecation windows.
- **Post-1.0 / policy-first:** 3 (schema-compat policy), 4b (Avro maps).

Tell me which rows to action and I'll implement them on the same test-first, incremental-push track as
the rest of Tier 2. 1b/2b I'd want to do as their own reviewed change given they touch the settlement
path.
