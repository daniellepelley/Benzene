using Benzene.Abstractions.Info;

namespace Benzene.Core.MessageHandlers.Info;

public class BlankApplicationInfo : IApplicationInfo
{
    public string Name => string.Empty;
    public string Description => string.Empty;
    public string Version => string.Empty;
}
