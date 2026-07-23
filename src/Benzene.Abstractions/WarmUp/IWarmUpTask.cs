using Benzene.Abstractions.DI;

namespace Benzene.Abstractions.WarmUp;

/// <summary>
/// A unit of cold-start warm-up work, run once during host initialization (the Lambda/Functions INIT
/// phase, before any invocation) to JIT and pre-build the framework internals a message would
/// otherwise pay for on its first call - the per-type serializer machinery, validators, etc.
/// </summary>
/// <remarks>
/// Warm-up is deliberately <b>invisible</b>: a task warms internals directly and does <b>not</b>
/// dispatch a message through the handler pipeline, so it produces no logs, metrics, traces, or
/// handler side-effects. It runs on a throwaway DI scope and is best-effort - a task that throws is
/// swallowed by the runner (a failed warm just means the first real message pays that cost).
/// </remarks>
public interface IWarmUpTask
{
    /// <summary>Warms this task's internals using the given (scoped) resolver.</summary>
    void WarmUp(IServiceResolver resolver);
}
