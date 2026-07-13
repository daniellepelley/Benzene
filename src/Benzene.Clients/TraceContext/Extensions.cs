namespace Benzene.Clients.TraceContext;

/// <summary>
/// Provides the <see cref="WithW3CTraceContext"/> extension for <see cref="ClientBuilder"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Stamps the current <see cref="System.Diagnostics.Activity"/>'s W3C <c>traceparent</c>/<c>tracestate</c>
    /// onto outgoing message headers, so the receiving service can continue the same distributed trace.
    /// </summary>
    /// <param name="source">The client builder to add the decorator to.</param>
    /// <returns>The client builder, for method chaining.</returns>
    public static ClientBuilder WithW3CTraceContext(this ClientBuilder source)
    {
        return source.WithDependencyWrapper(new TraceContextBenzeneMessageClientWrapper());
    }
}
