using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Saga;

/// <summary>
/// A saga step whose forward action produces a <typeparamref name="T"/> result, with an optional
/// compensation that undoes the effect using that result.
/// </summary>
/// <typeparam name="T">The type of the forward action's payload.</typeparam>
public class SagaStep<T> : ISagaStep
{
    private readonly Func<SagaContext, Task<IBenzeneResult<T>>> _forward;
    private readonly Func<SagaContext, T, Task<IBenzeneResult>>? _compensate;
    private readonly string? _key;
    private IBenzeneResult<T>? _result;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStep{T}"/> class.
    /// </summary>
    /// <param name="forward">The forward action, run with the saga context.</param>
    /// <param name="compensate">The optional compensation, given the context and the forward result; omit for a step with no effect to undo.</param>
    /// <param name="key">An optional explicit context key to publish the result under.</param>
    public SagaStep(Func<SagaContext, Task<IBenzeneResult<T>>> forward,
        Func<SagaContext, T, Task<IBenzeneResult>>? compensate = null, string? key = null)
    {
        _forward = forward;
        _compensate = compensate;
        _key = key;
    }

    /// <inheritdoc />
    public SagaStepState State { get; private set; } = SagaStepState.Pending;

    /// <inheritdoc />
    public IBenzeneResult? Result => _result;

    /// <inheritdoc />
    public Exception? Exception { get; private set; }

    /// <inheritdoc />
    public async Task ExecuteAsync(SagaContext context)
    {
        try
        {
            _result = await _forward(context);
            State = _result.IsSuccessful ? SagaStepState.Succeeded : SagaStepState.Failed;
        }
        catch (Exception ex)
        {
            Exception = ex;
            _result = BenzeneResult.Set<T>(BenzeneResultStatus.UnexpectedError, false);
            State = SagaStepState.Failed;
        }
    }

    /// <inheritdoc />
    public void Publish(SagaContext context)
    {
        if (State == SagaStepState.Succeeded && _result != null)
        {
            context.Set(_result.Payload, _key);
        }
    }

    /// <inheritdoc />
    public async Task<bool> CompensateAsync(SagaContext context)
    {
        // Only a step that actually succeeded created an effect worth undoing.
        if (State != SagaStepState.Succeeded)
        {
            return true;
        }

        // A succeeded step with no compensation is treated as "nothing to undo" - author a
        // compensation for any step that creates a side effect.
        if (_compensate == null)
        {
            State = SagaStepState.RolledBack;
            return true;
        }

        try
        {
            var compensationResult = await _compensate(context, _result!.Payload);
            if (compensationResult.IsSuccessful)
            {
                State = SagaStepState.RolledBack;
                return true;
            }
        }
        catch (Exception ex)
        {
            Exception = ex;
        }

        State = SagaStepState.CompensationFailed;
        return false;
    }
}
