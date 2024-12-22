using Benzene.Core.Correlation;

namespace Benzene.Core.DI;

public class CoreRegistrations : RegistrationsBase
{
    public CoreRegistrations()
    {
        Add(".AddBenzene()", Extensions.AddBenzene);
        Add(".AddBenzeneMessage()", Extensions.AddBenzeneMessage);
        Add(".AddCorrelationId()", x => x.AddCorrelationId());
        Add(".SetApplicationInfo(<name>, <version>, <description>)", x => x.SetApplicationInfo("", "", ""));
    }
}
