using System.Threading;

namespace Benzene.Abstractions.DI;

/// <summary>
/// Scope-level access to the ambient <see cref="CancellationToken"/> for the current unit of work,
/// so any component resolved from the scope (a health check, a handler, an outbound client) can
/// observe cancellation without the pipeline threading a token through every method signature.
/// Mirrors how ASP.NET Core exposes the request-aborted token via <c>IHttpContextAccessor</c>.
/// </summary>
/// <remarks>
/// Registered scoped. Defaults to <see cref="CancellationToken.None"/> until a transport (or a
/// component such as the health-check processor) seeds it for the scope. Read-only here; the seeding
/// side sets it via the concrete accessor.
/// </remarks>
public interface ICancellationTokenAccessor
{
    /// <summary>The cancellation token for the current scope, or <see cref="CancellationToken.None"/> when none has been set.</summary>
    CancellationToken CancellationToken { get; }
}
