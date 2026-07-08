# Test Helpers Refactoring Plan

## Problem
Test helper code exists in production packages, which:
1. Pollutes the public API surface
2. Ships unnecessary code to production users
3. Is unprofessional for a 1.0 release

## Test Helpers in Production Packages

### Core Packages:
- `Benzene.Core.MessageHandlers/BenzeneMessage/TestHelpers/BenzeneTestHostExtensions.cs`
- `Benzene.Core.MessageHandlers/BenzeneMessage/TestHelpers/MessageBuilderExtensions.cs`
- `Benzene.Core.Messages/BenzeneMessage/TestHelpers/BenzeneTestHostExtensions.cs`
- `Benzene.Core.Messages/BenzeneMessage/TestHelpers/MessageBuilderExtensions.cs`

### AWS Lambda Packages:
- `Benzene.Aws.Lambda.ApiGateway/BenzeneTestHostExtensions.cs`
- `Benzene.Aws.Lambda.Kafka/TestHelpers/MessageBuilderExtensions.cs`
- `Benzene.Aws.Lambda.Sns/TestHelpers/MessageBuilderExtensions.cs`
- `Benzene.Aws.Lambda.Sqs/TestHelpers/BenzeneTestHostExtensions.cs`
- `Benzene.Aws.Lambda.Sqs/TestHelpers/MessageBuilderExtensions.cs`

### AWS Packages:
- `Benzene.Aws.Sqs/TestHelpers/MessageBuilderExtensions.cs`

### Azure Packages:
- `Benzene.Azure.Kafka/TestHelpers/MessageBuilderExtensions.cs`
- `Benzene.Azure.EventHub/Function/TestHelpers/MessageBuilderExtensions.cs`

## Existing Test Package
`Benzene.Testing` already exists and has:
- `HttpBuilder.cs`
- `MessageBuilder.cs`
- `MessageBuilderExtensions.cs`

`Benzene.Tools` also exists with some AWS-specific test utilities.

## Proposed Solution

### Option A: Move to Benzene.Testing (Recommended)
1. Move all TestHelpers to `Benzene.Testing` package
2. Organize by adapter type:
   - `Benzene.Testing/Aws/Lambda/`
   - `Benzene.Testing/Azure/`
   - `Benzene.Testing/Core/`
3. Update project references in test projects
4. Remove TestHelpers folders from production packages

### Option B: Create Per-Adapter Test Packages
1. Create `Benzene.Aws.Lambda.Testing`
2. Create `Benzene.Azure.Testing`
3. Keep core test helpers in `Benzene.Testing`

Recommendation: **Option A** - simpler, single test package

## Implementation Steps

1. **Phase 1: Consolidate**
   - Copy all test helpers to `Benzene.Testing` in organized folders
   - Ensure namespaces are consistent

2. **Phase 2: Update Tests**
   - Update all test project references
   - Update using statements
   - Verify tests still pass

3. **Phase 3: Remove from Production**
   - Delete TestHelpers folders from production packages
   - Remove BenzeneTestHostExtensions from production packages

4. **Phase 4: Verify**
   - Build all packages
   - Run full test suite
   - Verify no test helpers in production package DLLs

## Effort Estimate
- Phase 1: 2-3 hours
- Phase 2: 2-4 hours (depends on number of test files)
- Phase 3: 1 hour
- Phase 4: 1-2 hours

**Total: 6-10 hours**

## BLOCKING for 1.0
This must be completed before 1.0 release. Test helpers in production packages is unprofessional and increases package size unnecessarily.
