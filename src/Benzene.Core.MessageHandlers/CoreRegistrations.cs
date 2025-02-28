using Benzene.Core.DI;
using Benzene.Core.MessageHandlers.DI;

namespace Benzene.Core.MessageHandlers;

public class CoreRegistrations : RegistrationsBase
{
    public CoreRegistrations()
    {
        Add(".AddBenzene()", DI.Extensions.AddBenzene);
        Add(".AddBenzeneMessage()", DI.Extensions.AddBenzeneMessage);
        Add(".SetApplicationInfo(<name>, <version>, <description>)", x => x.SetApplicationInfo("", "", ""));
    }
}
