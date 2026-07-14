using Benzene.Abstractions.MessageHandlers.Info;

namespace Benzene.Core.MessageHandlers.Info;

/// <summary>
/// Default fallback <see cref="IApplicationInfo"/> registered by <c>AddBenzene</c> so an
/// <see cref="IApplicationInfo"/> is always resolvable, even if the application never calls
/// <c>SetApplicationInfo</c>. Every property is an empty string.
/// </summary>
public class BlankApplicationInfo : IApplicationInfo
{
    /// <inheritdoc />
    public string Name => string.Empty;

    /// <inheritdoc />
    public string Description => string.Empty;

    /// <inheritdoc />
    public string Version => string.Empty;
}
