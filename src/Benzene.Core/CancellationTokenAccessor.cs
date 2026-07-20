using System.Threading;
using Benzene.Abstractions.DI;

namespace Benzene.Core;

/// <summary>
/// Default scoped <see cref="ICancellationTokenAccessor"/> - a mutable holder a transport (or a
/// component such as the health-check processor) sets for the scope, and any resolved component
/// reads. Registered scoped, so each unit of work gets its own; defaults to
/// <see cref="System.Threading.CancellationToken.None"/>.
/// </summary>
public class CancellationTokenAccessor : ICancellationTokenAccessor
{
    /// <inheritdoc />
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}
