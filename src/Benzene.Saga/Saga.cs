namespace Benzene.Saga;

/// <summary>
/// An in-code orchestrator for a distributed transaction: an ordered list of stages, each a group
/// of steps run concurrently. Runs stages in order, threading each stage's results into a shared
/// <see cref="SagaContext"/> for later stages; if any stage fails, every effect created so far is
/// compensated in reverse order, leaving the system back at its starting state so the saga can be
/// retried. It is all-or-nothing: it either completes in full or rolls back in full.
/// </summary>
public class Saga
{
    private readonly IReadOnlyList<Stage> _stages;

    internal Saga(IReadOnlyList<Stage> stages)
    {
        _stages = stages;
    }

    /// <summary>
    /// Runs the saga. Executes each stage in order; on the first stage failure, compensates every
    /// completed effect in reverse (last-in, first-out) order and returns a rolled-back result.
    /// </summary>
    /// <returns>The saga's outcome.</returns>
    public async Task<SagaResult> RunAsync()
    {
        var context = new SagaContext();
        var completedStages = new List<Stage>();

        for (var i = 0; i < _stages.Count; i++)
        {
            var stage = _stages[i];

            if (await stage.ExecuteAsync(context))
            {
                stage.Publish(context);
                completedStages.Add(stage);
                continue;
            }

            // Stage i failed. Roll back this stage's concurrently-succeeded steps first, then every
            // completed stage newest-first, so effects are undone in the reverse of the order they
            // were created.
            var rollbackClean = await RollBackAsync(context, completedStages, stage);

            var failedStep = stage.Steps.FirstOrDefault(step => step.State == SagaStepState.Failed);
            var compensationFailures = CollectCompensationFailures(completedStages, stage);

            return new SagaResult(
                rollbackClean ? SagaOutcome.RolledBack : SagaOutcome.PartiallyRolledBack,
                i,
                failedStep?.Result,
                failedStep?.Exception,
                compensationFailures);
        }

        return new SagaResult(SagaOutcome.Succeeded, null, null, null, Array.Empty<ISagaStep>());
    }

    private static async Task<bool> RollBackAsync(SagaContext context, List<Stage> completedStages, Stage failedStage)
    {
        var clean = await failedStage.CompensateAsync(context);

        for (var j = completedStages.Count - 1; j >= 0; j--)
        {
            var stageClean = await completedStages[j].CompensateAsync(context);
            clean = clean && stageClean;
        }

        return clean;
    }

    private static IReadOnlyList<ISagaStep> CollectCompensationFailures(List<Stage> completedStages, Stage failedStage)
    {
        return completedStages
            .Append(failedStage)
            .SelectMany(stage => stage.Steps)
            .Where(step => step.State == SagaStepState.CompensationFailed)
            .ToArray();
    }
}
