using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IDefaultStatuses"/> implementation, mapping each status to the corresponding
/// well-known value on <see cref="BenzeneResultStatus"/>.
/// </summary>
public class DefaultStatuses : IDefaultStatuses
{
    /// <inheritdoc />
    public string ValidationError => BenzeneResultStatus.ValidationError;

    /// <inheritdoc />
    public string NotFound => BenzeneResultStatus.NotFound;

    /// <inheritdoc />
    public string BadRequest => BenzeneResultStatus.BadRequest;
}
