using Benzene.Core.DI;

namespace Benzene.Aws.Lambda.EventBridge;

public class EventBridgeRegistrations : RegistrationsBase
{
    public EventBridgeRegistrations()
    {
        Add(".AddEventBridge()", x => x.AddEventBridge());
    }
}
