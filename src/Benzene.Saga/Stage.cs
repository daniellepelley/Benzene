namespace Benzene.Saga;

/// <summary>
/// A group of <see cref="ISagaStep"/>s that run concurrently as one all-or-nothing unit within a
/// <see cref="Saga"/>. The stage succeeds only if every step succeeds.
/// </summary>
public class Stage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Stage"/> class.
    /// </summary>
    /// <param name="steps">The steps that make up this stage.</param>
    public Stage(IReadOnlyList<ISagaStep> steps)
    {
        Steps = steps;
    }

    /// <summary>Gets the steps in this stage.</summary>
    public IReadOnlyList<ISagaStep> Steps { get; }

    /// <summary>
    /// Runs every step's forward action concurrently (awaiting them all, even if one fails early)
    /// and returns whether the whole stage succeeded.
    /// </summary>
    /// <param name="context">The saga context.</param>
    /// <returns><c>true</c> if every step succeeded; otherwise <c>false</c>.</returns>
    public async Task<bool> ExecuteAsync(SagaContext context)
    {
        await Task.WhenAll(Steps.Select(step => step.ExecuteAsync(context)));
        return Steps.All(step => step.State == SagaStepState.Succeeded);
    }

    /// <summary>Publishes every succeeded step's result into the context. Call only after the stage fully succeeds.</summary>
    /// <param name="context">The saga context.</param>
    public void Publish(SagaContext context)
    {
        foreach (var step in Steps)
        {
            step.Publish(context);
        }
    }

    /// <summary>
    /// Compensates this stage's succeeded steps concurrently (best effort - every compensation is
    /// attempted regardless of whether an earlier one failed).
    /// </summary>
    /// <param name="context">The saga context.</param>
    /// <returns><c>true</c> if every compensation succeeded (or there was nothing to undo); otherwise <c>false</c>.</returns>
    public async Task<bool> CompensateAsync(SagaContext context)
    {
        var results = await Task.WhenAll(Steps.Select(step => step.CompensateAsync(context)));
        return results.All(ok => ok);
    }
}
