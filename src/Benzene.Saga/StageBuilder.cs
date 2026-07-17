namespace Benzene.Saga;

/// <summary>
/// Fluent builder for a stage - a group of steps that run concurrently as one all-or-nothing unit.
/// </summary>
public class StageBuilder
{
    private readonly List<ISagaStep> _steps = new();

    /// <summary>
    /// Adds a step producing a <typeparamref name="T"/> result to this stage.
    /// </summary>
    /// <typeparam name="T">The type of the step's forward payload.</typeparam>
    /// <param name="configure">Configures the step (forward action and optional compensation).</param>
    /// <returns>This builder, for chaining.</returns>
    public StageBuilder Step<T>(Action<StepBuilder<T>> configure)
    {
        var stepBuilder = new StepBuilder<T>();
        configure(stepBuilder);
        _steps.Add(stepBuilder.Build());
        return this;
    }

    internal Stage Build()
    {
        if (_steps.Count == 0)
        {
            throw new InvalidOperationException("A saga stage requires at least one step.");
        }

        return new Stage(_steps);
    }
}
