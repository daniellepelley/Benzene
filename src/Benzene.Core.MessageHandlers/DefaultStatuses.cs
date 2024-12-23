using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

public class DefaultStatuses : IDefaultStatuses
{
    public string ValidationError => BenzeneResultStatus.ValidationError;
    public string NotFound => BenzeneResultStatus.NotFound;
}