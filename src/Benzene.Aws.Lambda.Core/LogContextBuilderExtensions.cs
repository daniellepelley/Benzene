using System;
using System.Linq;
using Benzene.Abstractions.Logging;
using Benzene.Aws.Lambda.Core.AwsEventStream;

namespace Benzene.Aws.Lambda.Core;

/// <summary>
/// Provides extension methods for adding AWS Lambda-specific fields to a log context builder.
/// </summary>
/// <remarks>
/// Used with <c>UseLogResult</c>/<c>UseLogContext</c> (from <c>Benzene.Core.Middleware</c>), e.g.
/// <c>.UseLogResult(x =&gt; x.WithRequestId().WithApplication())</c>.
/// </remarks>
public static class LogContextBuilderExtensions
{
    /// <summary>
    /// Adds the AWS Lambda request ID to the log context.
    /// </summary>
    /// <param name="source">The log context builder to extend.</param>
    /// <returns>The log context builder for method chaining.</returns>
    [Obsolete("Superseded by the portable Benzene.Diagnostics.EnrichmentExtensions.UseBenzeneEnrichment(), " +
        "which attaches the equivalent invocationId key on every platform, not just AWS Lambda.")]
    public static ILogContextBuilder<AwsEventStreamContext> WithRequestId(this ILogContextBuilder<AwsEventStreamContext> source)
    {
        return source.OnRequest("requestId", (_, context) => context.LambdaContext.AwsRequestId);
    }

    /// <summary>
    /// Adds the Lambda function's application name to the log context, derived from the function name
    /// by stripping any <c>::</c>-delimited handler suffix.
    /// </summary>
    /// <param name="source">The log context builder to extend.</param>
    /// <returns>The log context builder for method chaining.</returns>
    [Obsolete("Superseded by the portable Benzene.Diagnostics.EnrichmentExtensions.UseBenzeneEnrichment(); " +
        "for a transport-agnostic application name use Benzene.Core.MessageHandlers.LogContextBuilderExtensions.WithApplication() (IApplicationInfo-based) instead.")]
    public static ILogContextBuilder<AwsEventStreamContext> WithApplication(this ILogContextBuilder<AwsEventStreamContext> source)
    {
        return source.OnRequest("application", (_, context) => GetApplicationName(context.LambdaContext.FunctionName));
    }

    private static string GetApplicationName(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return input.Split("::", StringSplitOptions.RemoveEmptyEntries).First();
    }

}
