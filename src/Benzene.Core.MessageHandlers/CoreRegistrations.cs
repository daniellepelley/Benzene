using Benzene.Core.DI;

namespace Benzene.Core.MessageHandlers;

public class CoreRegistrations : RegistrationsBase
{
    public CoreRegistrations()
    {
        Add(".AddBenzene()", Extensions.AddBenzene);
        Add(".AddBenzeneMessage()", Extensions.AddBenzeneMessage);
        Add(".SetApplicationInfo(<name>, <version>, <description>)", x => x.SetApplicationInfo("", "", ""));
    }
}
