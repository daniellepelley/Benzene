namespace Benzene.Core.Middleware;

/// <summary>
/// Runs a batch of per-record work concurrently with an optional ceiling on how many run at once.
/// This is the shared primitive behind every batch fan-out application's opt-in
/// <c>MaxDegreeOfParallelism</c> knob: with no ceiling it behaves exactly like the plain
/// <c>Select(...).ToArray()</c> + <see cref="Task.WhenAll(System.Threading.Tasks.Task[])"/> pattern
/// it replaces (every record starts at once), and with a ceiling it caps concurrent execution so a
/// large batch can't start hundreds of pipeline runs - and hundreds of scoped
/// downstream resources (DB connections, HTTP clients) - simultaneously.
/// </summary>
/// <remarks>
/// Results are returned in the same order as <paramref name="source"/> regardless of completion order,
/// so a caller's positional/filtering logic over the results is unaffected by the concurrency setting.
/// Semantics otherwise match <see cref="Task.WhenAll{TResult}(System.Threading.Tasks.Task{TResult}[])"/>:
/// every record's task is awaited, and a faulted task surfaces once they have all completed.
/// </remarks>
public static class BoundedFanOut
{
    /// <summary>
    /// Projects each element of <paramref name="source"/> through <paramref name="body"/> concurrently,
    /// capping concurrency at <paramref name="maxDegreeOfParallelism"/>, and returns the results in
    /// source order.
    /// </summary>
    /// <typeparam name="TSource">The batch record type.</typeparam>
    /// <typeparam name="TResult">The per-record result type.</typeparam>
    /// <param name="source">The batch records to process.</param>
    /// <param name="body">The async work to run per record.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The maximum number of records processed at once. <c>null</c> or any value &lt;= 0 means
    /// unbounded (every record starts at once) - the default, behavior-preserving mode.
    /// </param>
    /// <returns>The per-record results, in the same order as <paramref name="source"/>.</returns>
    public static Task<TResult[]> WhenAllAsync<TSource, TResult>(
        IEnumerable<TSource> source, Func<TSource, Task<TResult>> body, int? maxDegreeOfParallelism)
    {
        if (!IsBounded(maxDegreeOfParallelism))
        {
            return Task.WhenAll(source.Select(body).ToArray());
        }

        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism!.Value);
        return RunGatedAsync(source, body, semaphore);
    }

    /// <summary>
    /// Runs <paramref name="body"/> over each element of <paramref name="source"/> concurrently,
    /// capping concurrency at <paramref name="maxDegreeOfParallelism"/>.
    /// </summary>
    /// <typeparam name="TSource">The batch record type.</typeparam>
    /// <param name="source">The batch records to process.</param>
    /// <param name="body">The async work to run per record.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The maximum number of records processed at once. <c>null</c> or any value &lt;= 0 means
    /// unbounded (every record starts at once) - the default, behavior-preserving mode.
    /// </param>
    public static Task WhenAllAsync<TSource>(
        IEnumerable<TSource> source, Func<TSource, Task> body, int? maxDegreeOfParallelism)
    {
        // Reuse the result-returning overload with a sentinel result so there's one gating
        // implementation to reason about; the throwaway bool[] is negligible against per-record
        // pipeline work.
        return WhenAllAsync(source, async item =>
        {
            await body(item);
            return true;
        }, maxDegreeOfParallelism);
    }

    private static async Task<TResult[]> RunGatedAsync<TSource, TResult>(
        IEnumerable<TSource> source, Func<TSource, Task<TResult>> body, SemaphoreSlim semaphore)
    {
        using (semaphore)
        {
            var tasks = source.Select(async item =>
                {
                    await semaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        return await body(item).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                })
                .ToArray();

            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Whether a configured degree-of-parallelism actually bounds anything (a positive value).
    /// <c>null</c> and non-positive values are treated as "unbounded".
    /// </summary>
    internal static bool IsBounded(int? maxDegreeOfParallelism)
        => maxDegreeOfParallelism is > 0;
}
