using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

public class DefaultStatuses : IDefaultStatuses
{
    public string ValidationError => ServiceResultStatus.ValidationError;
    public string NotFound => ServiceResultStatus.NotFound;
}