# ToDelete Folder Refactoring Plan

**Status: resolved (Option A taken).** The `ToDelete/` folder no longer exists — `IMessageResult`
(`src/Benzene.Abstractions.MessageHandlers/IMessageResult.cs`) and `IHasMessageResult`
(`.../IHasMessageResult.cs`) now live directly in `Benzene.Abstractions.MessageHandlers`, their
proper home given they're part of the public abstraction surface every transport package depends on.
All 7 usage sites listed below were verified still intact and compiling correctly (2026-07-14). Added
XML `<summary>` documentation to both interfaces, which was the one remaining gap from Option A's
checklist. No further action needed; kept for historical record.

## Problem (historical — see Status above)
The `src/Benzene.Abstractions.MessageHandlers/ToDelete/` folder contains public interfaces that are actively used across the codebase:
- `IMessageResult`
- `IHasMessageResult`

## Current Usage
These interfaces are implemented/used in:
1. `Benzene.Core.MessageHandlers/MessageResult.cs`
2. `Benzene.Aws.Lambda.Sns/SnsRecordContext.cs`
3. `Benzene.Aws.Lambda.Kafka/KafkaContext.cs`
4. `Benzene.Core.MessageHandlers/MessageMessageHandlerResultSetterBase.cs`
5. `Benzene.Azure.Function.Kafka/KafkaContext.cs`
6. `Benzene.Kafka.Core/KafkaMessage/KafkaRecordContext.cs`
7. `Benzene.Grpc/GrpcContext.cs`

## Options

### Option A: Keep Interfaces (Rename/Move)
If these interfaces are still needed:
1. Move them out of `ToDelete` folder to proper location
2. Rename if needed for clarity
3. Add XML documentation
4. Consider if they belong in Abstractions or should be internal

### Option B: Remove Interfaces (Refactor)
If these interfaces should be removed:
1. Identify what functionality they provide
2. Find alternative patterns (e.g., use generic context properties)
3. Refactor all 7 usage locations
4. This is a BREAKING CHANGE - must be done before 1.0

## Recommendation
**Decision needed from maintainer:** Are these interfaces deprecated or still part of the design?

- If deprecated: Complete refactoring BEFORE 1.0 (breaking change acceptable pre-1.0)
- If still valid: Move out of ToDelete and document properly

## Impact on 1.0 Release
~~**BLOCKING** - Cannot ship 1.0 with interfaces in a "ToDelete" folder. Must resolve before release.~~
Resolved — no longer blocking.
