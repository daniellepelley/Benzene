namespace Benzene.Abstractions.Hosting;

/// <summary>
/// Scoped mutable holder that carries the current invocation's <see cref="IBenzeneInvocation"/> from
/// the pipeline middleware that creates it (populated once per invocation) to wherever it's injected.
/// </summary>
/// <remarks>
/// Application code should depend on <see cref="IBenzeneInvocation"/> directly; this accessor exists
/// only so hosting-platform middleware has somewhere to put the invocation it builds.
/// </remarks>
public interface IBenzeneInvocationAccessor
{
    /// <summary>Gets or sets the current invocation.</summary>
    IBenzeneInvocation? Invocation { get; set; }
}
