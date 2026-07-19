using Benzene.Saga;
using Xunit;

namespace Benzene.Example.Saga.Tests;

/// <summary>
/// Drives the example's <see cref="SignupSaga"/> directly (no host/transport - the example itself
/// is a plain console app over <c>Benzene.Saga</c>) to prove out the saga cross-cutting concern
/// end-to-end: a multi-stage, partly-parallel transaction either completes in full or rolls back in
/// full, leaving no orphaned records.
/// </summary>
public class SignupSagaTest
{
    [Fact]
    public async Task RunAsync_EveryStageSucceeds_CompletesAndPersistsAllFourRecords()
    {
        var store = new Store();
        var api = new SignupApi(store);

        var result = await SignupSaga.Build(api, "Acme Ltd").RunAsync();

        Assert.Equal(SagaOutcome.Succeeded, result.Outcome);
        Assert.True(result.IsSuccess);
        Assert.Null(result.FailedStageIndex);
        Assert.Equal(4, store.Snapshot().Count);
    }

    [Fact]
    public async Task RunAsync_FinalStageFails_RollsBackEveryEarlierEffect()
    {
        var store = new Store();
        var api = new SignupApi(store) { FailAt = "rbac-role" };

        var result = await SignupSaga.Build(api, "Acme Ltd").RunAsync();

        Assert.Equal(SagaOutcome.RolledBack, result.Outcome);
        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.FailedStageIndex);
        Assert.Empty(store.Snapshot());
    }

    [Fact]
    public async Task RunAsync_MiddleStageFails_RollsBackTheParallelFirstStageToo()
    {
        var store = new Store();
        var api = new SignupApi(store) { FailAt = "user" };

        var result = await SignupSaga.Build(api, "Acme Ltd").RunAsync();

        Assert.Equal(SagaOutcome.RolledBack, result.Outcome);
        Assert.Equal(1, result.FailedStageIndex);
        // Stage 1's two parallel steps (tenant, okta-company) must both be compensated even though
        // the failure happened one stage later - this is the case a naive "only unwind the current
        // stage" implementation would get wrong.
        Assert.Empty(store.Snapshot());
    }

    [Fact]
    public async Task RunAsync_FirstStageParallelStepFails_RollsBackAnySiblingThatAlreadySucceeded()
    {
        var store = new Store();
        var api = new SignupApi(store) { FailAt = "okta-company" };

        var result = await SignupSaga.Build(api, "Acme Ltd").RunAsync();

        Assert.Equal(SagaOutcome.RolledBack, result.Outcome);
        Assert.Equal(0, result.FailedStageIndex);
        // The sibling parallel step (tenant) may have already succeeded by the time okta-company
        // fails - it must be compensated too, not left orphaned.
        Assert.Empty(store.Snapshot());
    }
}
