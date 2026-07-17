using Benzene.Abstractions.Results;

namespace Benzene.Saga;

/// <summary>
/// Fluent builder for a single saga step producing a <typeparamref name="T"/> result.
/// </summary>
/// <typeparam name="T">The type of the forward action's payload.</typeparam>
public class StepBuilder<T>
{
    private Func<SagaContext, Task<IBenzeneResult<T>>>? _forward;
    private Func<SagaContext, T, Task<IBenzeneResult>>? _compensate;
    private string? _key;

    /// <summary>
    /// Sets the forward action - typically an <c>IBenzeneMessageSender.SendAsync(...)</c> call,
    /// which already returns an <see cref="IBenzeneResult{T}"/>, but any async action returning one
    /// works.
    /// </summary>
    /// <param name="forward">The forward action, given the saga context.</param>
    /// <returns>This builder, for chaining.</returns>
    public StepBuilder<T> Do(Func<SagaContext, Task<IBenzeneResult<T>>> forward)
    {
        _forward = forward;
        return this;
    }

    /// <summary>
    /// Sets the compensation that undoes the forward action, given the saga context and the forward
    /// result. Omit for a step that creates no effect to undo.
    /// </summary>
    /// <param name="compensate">The compensation action.</param>
    /// <returns>This builder, for chaining.</returns>
    public StepBuilder<T> Compensate(Func<SagaContext, T, Task<IBenzeneResult>> compensate)
    {
        _compensate = compensate;
        return this;
    }

    /// <summary>
    /// Sets an explicit context key to publish this step's result under - needed only when a stage
    /// produces more than one value of the same type. Defaults to <typeparamref name="T"/>'s name.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <returns>This builder, for chaining.</returns>
    public StepBuilder<T> Key(string key)
    {
        _key = key;
        return this;
    }

    internal ISagaStep Build()
    {
        if (_forward == null)
        {
            throw new InvalidOperationException("A saga step requires a forward action - call Do(...).");
        }

        return new SagaStep<T>(_forward, _compensate, _key);
    }
}
