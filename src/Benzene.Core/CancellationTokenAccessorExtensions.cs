using System.Threading;
using Benzene.Abstractions.DI;

namespace Benzene.Core;

/// <summary>
/// Helper for seeding the scope's ambient <see cref="CancellationTokenAccessor"/> from a transport's
/// real cancellation signal, so any component resolved from the scope (a handler, an outbound client,
/// a health check) can observe cancellation via <see cref="ICancellationTokenAccessor"/> without the
/// pipeline threading a <see cref="CancellationToken"/> through every method signature.
/// </summary>
public static class CancellationTokenAccessorExtensions
{
    /// <summary>
    /// Sets the scope's ambient cancellation token, if a <see cref="CancellationTokenAccessor"/> is
    /// registered and the token can actually be cancelled. A non-cancellable token
    /// (<see cref="CancellationToken.None"/>) is ignored, leaving the accessor at its default so the
    /// call is a no-op for transports without a real signal.
    /// </summary>
    /// <param name="serviceResolver">The scope to seed - normally the one the pipeline runs in.</param>
    /// <param name="cancellationToken">The transport's cancellation token for this unit of work.</param>
    public static void SeedCancellationToken(this IServiceResolver serviceResolver, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            return;
        }

        var accessor = serviceResolver.TryGetService<CancellationTokenAccessor>();
        if (accessor != null)
        {
            accessor.CancellationToken = cancellationToken;
        }
    }
}
