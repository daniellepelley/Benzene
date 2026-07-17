using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Results;
using Benzene.Saga;
using Xunit;

namespace Benzene.Test.Saga;

public class SagaTest
{
    private static Task<IBenzeneResult<string>> Ok(List<string> log, string tag, string value)
    {
        log.Add(tag);
        return Task.FromResult(BenzeneResult.Ok(value));
    }

    private static Task<IBenzeneResult<string>> Fail(List<string> log, string tag)
    {
        log.Add(tag);
        return Task.FromResult(BenzeneResult.ServiceUnavailable<string>());
    }

    private static Task<IBenzeneResult> Undo(List<string> log, string tag, bool succeeds = true)
    {
        log.Add(tag);
        return Task.FromResult(succeeds ? BenzeneResult.Ok() : BenzeneResult.ServiceUnavailable());
    }

    [Fact]
    public async Task RunAsync_AllStagesSucceed_ReturnsSucceeded_AndThreadsContextForward()
    {
        var log = new List<string>();

        var saga = new SagaBuilder()
            .Stage(stage => stage.Step<string>(step => step
                .Do(_ => Ok(log, "create-tenant", "tenant-1"))
                .Compensate((_, r) => Undo(log, $"undo-tenant:{r}"))))
            .Stage(stage => stage.Step<string>(step => step
                .Do(ctx => Ok(log, $"create-user:{ctx.Get<string>()}", "user-1"))))
            .Build();

        var result = await saga.RunAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(SagaOutcome.Succeeded, result.Outcome);
        // stage 2 read stage 1's published result; no compensation ran.
        Assert.Equal(new[] { "create-tenant", "create-user:tenant-1" }, log);
    }

    [Fact]
    public async Task RunAsync_ConcurrentStepsRunInParallelWithinAStage()
    {
        var barrier = new TaskCompletionSource();
        var bothStarted = 0;

        async Task<IBenzeneResult<string>> Waiter()
        {
            if (Interlocked.Increment(ref bothStarted) == 2)
            {
                barrier.SetResult();
            }
            await barrier.Task;
            return BenzeneResult.Ok("done");
        }

        var saga = new SagaBuilder()
            .Stage(stage => stage
                .Step<string>(step => step.Do(_ => Waiter()))
                .Step<string>(step => step.Do(_ => Waiter())))
            .Build();

        // If the two steps ran sequentially, the first would await a barrier only the second can
        // release, and this would deadlock/time out. Completing proves they ran concurrently.
        var completed = await Task.WhenAny(saga.RunAsync(), Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.IsType<Task<SagaResult>>(completed);
        Assert.True(((Task<SagaResult>)completed).Result.IsSuccess);
    }

    [Fact]
    public async Task RunAsync_StepFailsWithinStage_CompensatesSucceededSiblings_AndRollsBack()
    {
        var log = new List<string>();

        var saga = new SagaBuilder()
            .Stage(stage => stage
                .Step<string>(step => step
                    .Do(_ => Ok(log, "create-a", "a-1"))
                    .Compensate((_, r) => Undo(log, $"undo-a:{r}")))
                .Step<string>(step => step
                    .Do(_ => Fail(log, "create-b"))))
            .Build();

        var result = await saga.RunAsync();

        Assert.Equal(SagaOutcome.RolledBack, result.Outcome);
        Assert.Equal(0, result.FailedStageIndex);
        Assert.Contains("undo-a:a-1", log); // the succeeded sibling was compensated
    }

    [Fact]
    public async Task RunAsync_LaterStageFails_CompensatesCompletedStagesInReverseOrder()
    {
        var log = new List<string>();

        var saga = new SagaBuilder()
            .Stage(stage => stage.Step<string>(step => step
                .Do(_ => Ok(log, "s1", "1"))
                .Compensate((_, r) => Undo(log, "undo-s1"))))
            .Stage(stage => stage.Step<string>(step => step
                .Do(_ => Ok(log, "s2", "2"))
                .Compensate((_, r) => Undo(log, "undo-s2"))))
            .Stage(stage => stage.Step<string>(step => step
                .Do(_ => Fail(log, "s3"))))
            .Build();

        var result = await saga.RunAsync();

        Assert.Equal(SagaOutcome.RolledBack, result.Outcome);
        Assert.Equal(2, result.FailedStageIndex);
        // LIFO: s3 fails, then s2 undone, then s1 undone.
        Assert.Equal(new[] { "s1", "s2", "s3", "undo-s2", "undo-s1" }, log);
    }

    [Fact]
    public async Task RunAsync_CompensationItselfFails_ReturnsPartiallyRolledBack()
    {
        var saga = new SagaBuilder()
            .Stage(stage => stage.Step<string>(step => step
                .Do(_ => Task.FromResult(BenzeneResult.Ok("1")))
                .Compensate((_, _) => Task.FromResult(BenzeneResult.ServiceUnavailable())))) // undo fails
            .Stage(stage => stage.Step<string>(step => step
                .Do(_ => Task.FromResult(BenzeneResult.ServiceUnavailable<string>())))) // triggers rollback
            .Build();

        var result = await saga.RunAsync();

        Assert.Equal(SagaOutcome.PartiallyRolledBack, result.Outcome);
        Assert.Single(result.CompensationFailures);
        Assert.Equal(SagaStepState.CompensationFailed, result.CompensationFailures[0].State);
    }

    [Fact]
    public async Task RunAsync_ForwardThrows_TreatedAsFailure_AndRollsBackPriorStages()
    {
        var log = new List<string>();

        var saga = new SagaBuilder()
            .Stage(stage => stage.Step<string>(step => step
                .Do(_ => Ok(log, "s1", "1"))
                .Compensate((_, r) => Undo(log, "undo-s1"))))
            .Stage(stage => stage.Step<string>(step => step
                .Do(_ => throw new InvalidOperationException("boom"))))
            .Build();

        var result = await saga.RunAsync();

        Assert.Equal(SagaOutcome.RolledBack, result.Outcome);
        Assert.Equal(1, result.FailedStageIndex);
        Assert.IsType<InvalidOperationException>(result.FailureException);
        Assert.Contains("undo-s1", log);
    }

    [Fact]
    public async Task RunAsync_SucceededStepWithNoCompensation_RollsBackCleanly()
    {
        // A read-only/no-effect step that succeeds has no compensation; a later failure should still
        // report a clean RolledBack (nothing to undo for that step).
        var saga = new SagaBuilder()
            .Stage(stage => stage.Step<string>(step => step
                .Do(_ => Task.FromResult(BenzeneResult.Ok("read")))))
            .Stage(stage => stage.Step<string>(step => step
                .Do(_ => Task.FromResult(BenzeneResult.ServiceUnavailable<string>()))))
            .Build();

        var result = await saga.RunAsync();

        Assert.Equal(SagaOutcome.RolledBack, result.Outcome);
        Assert.Empty(result.CompensationFailures);
    }

    [Fact]
    public void Build_WithNoStages_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new SagaBuilder().Build());
    }

    [Fact]
    public void Build_StepWithNoForward_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SagaBuilder().Stage(stage => stage.Step<string>(_ => { })).Build());
    }
}
