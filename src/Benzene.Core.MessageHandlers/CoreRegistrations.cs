using Benzene.Core.DI;

namespace Benzene.Core.MessageHandlers;

public class CoreRegistrations : RegistrationsBase
{
    public CoreRegistrations()
    {
        // Add(".AddMessageHandlers(<assemblies>)", x => x.AddMessageHandlers(AppDomain.CurrentDomain.GetAssemblies()));
    }
}
