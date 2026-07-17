using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Results;
using Benzene.Saga;
using Xunit;

namespace Benzene.Test.Saga;

// The §7 fast-follows: an optional whole-saga retry policy and a pluggable ISagaStateStore.
public class SagaRetryAndStateStoreTest
{
    private static Task<IBenzeneResult<string>> Ok(string value) => Task.FromResult(BenzeneResult.Ok(value));
    private static Task<IBenzeneResult<string>> Fail() => Task.FromResult(BenzeneResult.ServiceUnavailable<string>());
    private static Task<IBenzeneResult> Undo() => Task.FromResult(BenzeneResult.Ok());

    // ---- Retry -----------------------------------------------------------------------------------

    [Fact]
    public async Task Retry_ReRunsAfterCleanRollback_AndSucceedsOnceTheFlakyStepRecovers()
    {
        var attempts = 0;
        var saga = new SagaBuilder()
            .Stage(s => s.Step<string>(step => step.Do(_ => Ok("a")).Compensate((_, _) => Undo())))
            .Stage(s => s.Step<string>(step => step.Do(_ =>
            {
                attempts++;
                return attempts < 2 ? Fail() : Ok("b"); // fails first attempt, succeeds on the second
            })))
            .Build();

        var result = await saga.RunAsync(new SagaRunOptions
        {
            RetryPolicy = new SagaRetryPolicy(maxAttempts: 3, delay: _ => Task.CompletedTask)
        });

        Assert.Equal(SagaOutcome.Succeeded, result.Outcome);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task Retry_ExhaustsAttempts_ReturnsRolledBack()
    {
        var attempts = 0;
        var saga = new SagaBuilder()
            .Stage(s => s.Step<string>(step => step.Do(_ => { attempts++; return Fail(); })))
            .Build();

        var result = await saga.RunAsync(new SagaRunOptions
        {
            RetryPolicy = new SagaRetryPolicy(maxAttempts: 3, delay: _ => Task.CompletedTask)
        });

        Assert.Equal(SagaOutcome.RolledBack, result.Outcome);
        Assert.Equal(3, attempts); // tried the configured maximum
    }

    [Fact]
    public async Task Retry_DoesNotRetry_OnPartiallyRolledBack()
    {
        // Stage 1 succeeds but its compensation fails; stage 2 fails -> rollback is not clean.
        var forwardAttempts = 0;
        var saga = new SagaBuilder()
            .Stage(s => s.Step<string>(step => step
                .Do(_ => Ok("a"))
                .Compensate((_, _) => Task.FromResult(BenzeneResult.ServiceUnavailable())))) // compensation fails
            .Stage(s => s.Step<string>(step => step.Do(_ => { forwardAttempts++; return Fail(); })))
            .Build();

        var result = await saga.RunAsync(new SagaRunOptions
        {
            RetryPolicy = new SagaRetryPolicy(maxAttempts: 5, delay: _ => Task.CompletedTask)
        });

        Assert.Equal(SagaOutcome.PartiallyRolledBack, result.Outcome);
        Assert.Equal(1, forwardAttempts); // not retried - orphaned effects must not be re-applied
    }

    // ---- State store -----------------------------------------------------------------------------

    [Fact]
    public async Task StateStore_RecordsStart_EachStageCompletion_AndSuccessfulFinish()
    {
        var store = new InMemorySagaStateStore();
        var saga = new SagaBuilder()
            .Stage(s => s.Step<string>(step => step.Do(_ => Ok("a"))))
            .Stage(s => s.Step<string>(step => step.Do(_ => Ok("b"))))
            .Build();

        await saga.RunAsync(new SagaRunOptions { SagaId = "saga-1", StateStore = store });

        var kinds = store.EventsFor("saga-1").Select(e => e.Kind).ToArray();
        Assert.Equal(new[]
        {
            SagaStateEventKind.Started,
            SagaStateEventKind.StageCompleted,
            SagaStateEventKind.StageCompleted,
            SagaStateEventKind.Finished
        }, kinds);

        var finished = store.EventsFor("saga-1").Single(e => e.Kind == SagaStateEventKind.Finished);
        Assert.Equal(SagaOutcome.Succeeded, finished.Result!.Outcome);
    }

    [Fact]
    public async Task StateStore_OnFailure_RecordsOnlyCompletedStages_AndRolledBackFinish()
    {
        var store = new InMemorySagaStateStore();
        var saga = new SagaBuilder()
            .Stage(s => s.Step<string>(step => step.Do(_ => Ok("a")).Compensate((_, _) => Undo())))
            .Stage(s => s.Step<string>(step => step.Do(_ => Fail())))
            .Build();

        await saga.RunAsync(new SagaRunOptions { SagaId = "saga-2", StateStore = store });

        var events = store.EventsFor("saga-2");
        Assert.Single(events.Where(e => e.Kind == SagaStateEventKind.StageCompleted)); // only stage 0
        Assert.Equal(0, events.Single(e => e.Kind == SagaStateEventKind.StageCompleted).StageIndex);
        Assert.Equal(SagaOutcome.RolledBack, events.Single(e => e.Kind == SagaStateEventKind.Finished).Result!.Outcome);
    }

    [Fact]
    public async Task StateStore_RecordsEachRetryAttempt()
    {
        var store = new InMemorySagaStateStore();
        var attempts = 0;
        var saga = new SagaBuilder()
            .Stage(s => s.Step<string>(step => step.Do(_ =>
            {
                attempts++;
                return attempts < 2 ? Fail() : Ok("a");
            })))
            .Build();

        await saga.RunAsync(new SagaRunOptions
        {
            SagaId = "saga-3",
            StateStore = store,
            RetryPolicy = new SagaRetryPolicy(maxAttempts: 3, delay: _ => Task.CompletedTask)
        });

        var startedAttempts = store.EventsFor("saga-3")
            .Where(e => e.Kind == SagaStateEventKind.Started)
            .Select(e => e.Attempt)
            .ToArray();
        Assert.Equal(new[] { 1, 2 }, startedAttempts); // one Started per attempt, sharing the saga id
    }

    [Fact]
    public async Task StateStore_GeneratesSagaId_WhenNoneSupplied()
    {
        var store = new InMemorySagaStateStore();
        var saga = new SagaBuilder()
            .Stage(s => s.Step<string>(step => step.Do(_ => Ok("a"))))
            .Build();

        await saga.RunAsync(new SagaRunOptions { StateStore = store });

        Assert.NotEmpty(store.Events);
        Assert.False(string.IsNullOrEmpty(store.Events[0].SagaId));
    }

    [Fact]
    public async Task ParameterlessRun_TouchesNoStore_AndBehavesAsBefore()
    {
        var saga = new SagaBuilder()
            .Stage(s => s.Step<string>(step => step.Do(_ => Ok("a"))))
            .Build();

        var result = await saga.RunAsync();

        Assert.Equal(SagaOutcome.Succeeded, result.Outcome);
    }
}
