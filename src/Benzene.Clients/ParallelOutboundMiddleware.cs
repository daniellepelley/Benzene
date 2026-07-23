using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Core.Middleware;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients;

/// <summary>
/// Sends one outbound message to several transports <b>concurrently</b>, instead of the default
/// awaited chain that sends to each in turn (a route's transport middleware are terminal, so a single
/// route otherwise holds a single transport). Each branch runs on its own cloned
/// <see cref="OutboundContext"/> - the outbound middleware (correlation id, W3C trace) write onto
/// <see cref="OutboundContext.Headers"/> and each branch sets its own <see cref="OutboundContext.Response"/>,
/// so sharing one context would race both. All branches are awaited together
/// (<see cref="BoundedFanOut"/>, so concurrency is capped when a limit is given), then the results are
/// aggregated <b>all-must-succeed</b>: success only if every branch succeeded, otherwise a single
/// failure whose errors name each failed transport. Egress converters always produce
/// <see cref="IBenzeneResult{Void}"/>, so the aggregate is an <see cref="IBenzeneResult{Void}"/> too.
/// </summary>
internal class ParallelOutboundMiddleware : IMiddleware<OutboundContext>
{
    private readonly IReadOnlyList<Branch> _branches;
    private readonly IServiceResolver _serviceResolver;
    private readonly int? _maxDegreeOfParallelism;

    /// <summary>Initializes a new instance of the <see cref="ParallelOutboundMiddleware"/> class.</summary>
    public ParallelOutboundMiddleware(IReadOnlyList<Branch> branches, IServiceResolver serviceResolver, int? maxDegreeOfParallelism)
    {
        _branches = branches;
        _serviceResolver = serviceResolver;
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    /// <inheritdoc />
    public string Name => "ParallelSend";

    /// <inheritdoc />
    public async Task HandleAsync(OutboundContext context, Func<Task> next)
    {
        var outcomes = await BoundedFanOut.WhenAllAsync(_branches, async branch =>
        {
            // A fresh context per branch (OutboundContext's ctor copies Headers), so concurrent
            // branches don't race the shared header dictionary or the single Response slot.
            var branchContext = new OutboundContext(context.Topic, context.Request, context.Headers);
            try
            {
                await branch.Pipeline.HandleAsync(branchContext, _serviceResolver);
                return new BranchOutcome(branch.Name, branchContext.Response as IBenzeneResult, null);
            }
            catch (Exception ex)
            {
                // Catch per branch rather than let the first failure abort the fan-out - every branch
                // still runs, and the aggregate can then name all of the ones that failed.
                return new BranchOutcome(branch.Name, null, ex);
            }
        }, _maxDegreeOfParallelism);

        var failures = outcomes
            .Where(outcome => outcome.Exception is not null || outcome.Result is null || !outcome.Result.IsSuccessful)
            .ToArray();

        if (failures.Length == 0)
        {
            context.Response = BenzeneResult.Ok<Void>();
            return;
        }

        var errors = failures.Select(FormatError).ToArray();
        context.Response = BenzeneResult.Set<Void>(BenzeneResultStatus.UnexpectedError, errors);
    }

    private static string FormatError(BranchOutcome outcome) => outcome.Exception is not null
        ? $"{outcome.Name}: {outcome.Exception.GetType().Name}: {outcome.Exception.Message}"
        : $"{outcome.Name}: {outcome.Result?.Status ?? "no response"}";

    /// <summary>One transport's send: its display name and the (single-transport) pipeline that runs it.</summary>
    internal sealed class Branch
    {
        public Branch(string name, IMiddlewarePipeline<OutboundContext> pipeline)
        {
            Name = name;
            Pipeline = pipeline;
        }

        public string Name { get; }
        public IMiddlewarePipeline<OutboundContext> Pipeline { get; }
    }

    private sealed class BranchOutcome
    {
        public BranchOutcome(string name, IBenzeneResult? result, Exception? exception)
        {
            Name = name;
            Result = result;
            Exception = exception;
        }

        public string Name { get; }
        public IBenzeneResult? Result { get; }
        public Exception? Exception { get; }
    }
}
