namespace Benzene.Saga;

/// <summary>
/// Fluent builder for a <see cref="Saga"/> - an ordered list of stages.
/// </summary>
public class SagaBuilder
{
    private readonly List<Stage> _stages = new();

    /// <summary>
    /// Adds a stage to the saga. Stages run in the order they are added; a later stage can read an
    /// earlier stage's results from the <see cref="SagaContext"/>.
    /// </summary>
    /// <param name="configure">Configures the stage's steps.</param>
    /// <returns>This builder, for chaining.</returns>
    public SagaBuilder Stage(Action<StageBuilder> configure)
    {
        var stageBuilder = new StageBuilder();
        configure(stageBuilder);
        _stages.Add(stageBuilder.Build());
        return this;
    }

    /// <summary>Builds the saga.</summary>
    /// <returns>The configured <see cref="Saga"/>.</returns>
    /// <exception cref="InvalidOperationException">No stages were added.</exception>
    public Saga Build()
    {
        if (_stages.Count == 0)
        {
            throw new InvalidOperationException("A saga requires at least one stage.");
        }

        return new Saga(_stages);
    }
}
