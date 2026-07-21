using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.Idempotency;

/// <summary>
/// Middleware that de-duplicates redelivered messages on an at-least-once transport. It derives an
/// idempotency key for each message, atomically claims it in an <see cref="IIdempotencyStore"/>, and
/// only invokes the rest of the pipeline (including the handler) the first time that key is seen.
/// Duplicates short-circuit without re-running the handler.
/// </summary>
/// <remarks>
/// <para>Place it early in the pipeline — before the handler, but typically after logging/tracing so
/// duplicates are still observable.</para>
/// <para>Outcome handling: if the handler throws, or reports failure via
/// <see cref="IHasMessageResult"/>, the claim is released so the transport's redelivery reprocesses
/// the message rather than the failure being permanently suppressed. Only a successful first attempt
/// is recorded as completed.</para>
/// </remarks>
/// <typeparam name="TContext">The transport-specific message context type.</typeparam>
public class IdempotencyMiddleware<TContext> : IMiddleware<TContext>
{
    private readonly IIdempotencyStore _store;
    private readonly IIdempotencyKeyStrategy<TContext> _keyStrategy;
    private readonly IdempotencyOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyMiddleware{TContext}"/> class.
    /// </summary>
    public IdempotencyMiddleware(
        IIdempotencyStore store,
        IIdempotencyKeyStrategy<TContext> keyStrategy,
        IdempotencyOptions options)
    {
        _store = store;
        _keyStrategy = keyStrategy;
        _options = options;
    }

    /// <inheritdoc />
    public string Name => nameof(IdempotencyMiddleware<TContext>);

    /// <inheritdoc />
    public async Task HandleAsync(TContext context, Func<Task> next)
    {
        var key = _keyStrategy.GetKey(context);
        if (key == null)
        {
            // No key derived -> this message opts out of de-duplication; process normally.
            await next();
            return;
        }

        var claim = await _store.TryClaimAsync(key);
        if (!claim.Claimed)
        {
            HandleDuplicate(context, claim.ExistingRecord!);
            return;
        }

        try
        {
            await next();
        }
        catch
        {
            // The handler threw. Release the claim so a redelivery can reprocess the message.
            await _store.ReleaseAsync(key);
            throw;
        }

        if (WasSuccessful(context))
        {
            await _store.CompleteAsync(key, true);
        }
        else
        {
            // The handler ran but reported failure. Release so the redelivery retries.
            await _store.ReleaseAsync(key);
        }
    }

    private void HandleDuplicate(TContext context, IdempotencyRecord existing)
    {
        if (existing.Status == IdempotencyStatus.InProgress
            && _options.InProgressBehavior == InProgressBehavior.Throw)
        {
            throw new IdempotencyConflictException(existing.Key);
        }

        // A completed duplicate (or an in-progress one under Skip): short-circuit without re-running
        // the handler. For transports that report completion via a message result, mark it successful
        // so the duplicate is acknowledged and removed from the queue rather than redelivered again.
        if (context is IHasMessageResult hasResult)
        {
            hasResult.MessageResult = BenzeneResult.Ok();
        }
    }

    private static bool WasSuccessful(TContext context)
    {
        // Prefer the pipeline's own result signal when the transport sets one; otherwise treat
        // "the handler did not throw" as success.
        if (context is IHasMessageResult { MessageResult: not null } hasResult)
        {
            return hasResult.MessageResult.IsSuccessful;
        }

        return true;
    }
}
