using Benzene.Abstractions.Hosting;

namespace Benzene.Core.Middleware;

/// <summary>
/// Default <see cref="IBenzeneInvocation"/> implementation, holding an invocation ID, platform
/// identifier, and a bag of native platform features keyed by type.
/// </summary>
public class BenzeneInvocation : IBenzeneInvocation
{
    private readonly IReadOnlyDictionary<Type, object> _features;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneInvocation"/> class.
    /// </summary>
    /// <param name="invocationId">The invocation identifier.</param>
    /// <param name="platform">The hosting platform identifier.</param>
    /// <param name="features">The native platform features available for this invocation, keyed by type.</param>
    public BenzeneInvocation(string invocationId, string platform, IReadOnlyDictionary<Type, object> features)
    {
        InvocationId = invocationId;
        Platform = platform;
        _features = features;
    }

    /// <inheritdoc />
    public string InvocationId { get; }

    /// <inheritdoc />
    public string Platform { get; }

    /// <inheritdoc />
    public T? GetFeature<T>() where T : class =>
        _features.TryGetValue(typeof(T), out var feature) ? feature as T : null;
}

/// <summary>
/// Default <see cref="IBenzeneInvocationAccessor"/> implementation: a plain scoped mutable holder.
/// </summary>
public class BenzeneInvocationAccessor : IBenzeneInvocationAccessor
{
    /// <inheritdoc />
    public IBenzeneInvocation? Invocation { get; set; }
}
