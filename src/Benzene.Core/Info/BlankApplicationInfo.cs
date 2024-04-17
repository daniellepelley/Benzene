using Benzene.Abstractions.Info;

namespace Benzene.Core.Info;

public class BlankApplicationInfo : IApplicationInfo
{
    public string Name => string.Empty;
    public string Description => string.Empty;
    public string Version => string.Empty;
}
