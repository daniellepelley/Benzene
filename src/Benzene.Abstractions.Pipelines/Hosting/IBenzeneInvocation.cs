namespace Benzene.Abstractions.Hosting;

/// <summary>
/// Exposes platform-neutral and platform-specific metadata for the current invocation, so a handler
/// can stay portable across hosts while still reaching native platform context (e.g. AWS's
/// <c>ILambdaContext</c> or ASP.NET Core's <c>HttpContext</c>) when it genuinely needs to.
/// </summary>
/// <remarks>
/// Resolve this as a scoped dependency; it is populated once per invocation by the hosting platform's
/// <c>UseBenzeneInvocation()</c> pipeline extension (e.g. in <c>Benzene.Aws.Lambda.Core</c> or
/// <c>Benzene.AspNet.Core</c>). Handlers that never call <see cref="GetFeature{T}"/> compile and run
/// unchanged on every platform; only the call site that opts into a specific feature type is tied to
/// that platform.
/// <para>
/// Like log-context enrichment (<c>UseLogResult</c>/<c>UseLogContext</c>), this is populated per pipeline,
/// not globally: call <c>UseBenzeneInvocation()</c> at whichever pipeline level you need it resolvable
/// from. It does not automatically flow into a nested sub-application that creates its own DI scope
/// (e.g. AWS's <c>UseBenzeneMessage</c>/per-message SQS or SNS batch dispatch) -- add it to that
/// inner pipeline too if you need it there.
/// </para>
/// </remarks>
public interface IBenzeneInvocation
{
    /// <summary>
    /// Gets an identifier for the current invocation (e.g. the AWS Lambda request ID or the ASP.NET
    /// Core trace identifier), unique enough for correlating logs and traces for this invocation.
    /// </summary>
    string InvocationId { get; }

    /// <summary>
    /// Gets the hosting platform identifier for the current invocation (e.g. "AwsLambda", "AspNet"),
    /// matching <see cref="IBenzeneApplicationBuilder.Platform"/>.
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Gets the native platform feature of the given type for this invocation (e.g. <c>ILambdaContext</c>
    /// or <c>HttpContext</c>), or <c>null</c> if this invocation's platform doesn't expose one.
    /// </summary>
    /// <typeparam name="T">The feature type to retrieve.</typeparam>
    T? GetFeature<T>() where T : class;
}
