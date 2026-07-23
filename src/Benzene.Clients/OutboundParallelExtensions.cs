using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Clients;

/// <summary>Pipeline extensions for sending one outbound message to several transports concurrently.</summary>
public static class OutboundParallelExtensions
{
    /// <summary>
    /// Sends the message to every named branch <b>concurrently</b> (unbounded) rather than one after
    /// another. Use it in an <c>AddOutboundRouting</c> route to fan a single topic out to several
    /// transports at once, e.g.
    /// <code>
    /// routing.Route("orders:create", p => p.UseParallel(
    ///     ("sqs", b => b.UseSqs(queueUrl)),
    ///     ("sns", b => b.UseSns(topicArn))));
    /// </code>
    /// The send succeeds only if every branch succeeds; otherwise the result is a single failure whose
    /// errors name each failed transport (all-must-succeed). This is a terminal send step - like the
    /// transport middleware it fans out to, it does not continue to any middleware added after it.
    /// </summary>
    /// <param name="app">The outbound route pipeline.</param>
    /// <param name="branches">Each transport's display name and its single-transport configuration.</param>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseParallel(
        this IMiddlewarePipelineBuilder<OutboundContext> app,
        params (string Name, Action<IMiddlewarePipelineBuilder<OutboundContext>> Configure)[] branches)
        => app.UseParallel(null, branches);

    /// <summary>
    /// As <see cref="UseParallel(IMiddlewarePipelineBuilder{OutboundContext},ValueTuple{string,Action{IMiddlewarePipelineBuilder{OutboundContext}}}[])"/>,
    /// but caps how many branches send at once (see <see cref="BoundedFanOut"/>). <c>null</c> or a
    /// value &lt;= 0 is unbounded (all branches start together).
    /// </summary>
    /// <param name="app">The outbound route pipeline.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of branches sending concurrently.</param>
    /// <param name="branches">Each transport's display name and its single-transport configuration.</param>
    public static IMiddlewarePipelineBuilder<OutboundContext> UseParallel(
        this IMiddlewarePipelineBuilder<OutboundContext> app,
        int? maxDegreeOfParallelism,
        params (string Name, Action<IMiddlewarePipelineBuilder<OutboundContext>> Configure)[] branches)
    {
        if (branches is null || branches.Length == 0)
        {
            throw new ArgumentException("UseParallel requires at least one branch.", nameof(branches));
        }

        var built = branches
            .Select(branch => new ParallelOutboundMiddleware.Branch(branch.Name, app.CreateMiddlewarePipeline(branch.Configure)))
            .ToArray();

        return app.Use(resolver => new ParallelOutboundMiddleware(built, resolver, maxDegreeOfParallelism));
    }
}
