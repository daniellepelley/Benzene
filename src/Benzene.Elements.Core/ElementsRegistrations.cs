using Benzene.Core.DI;
using Benzene.Elements.Core.Broadcast;

namespace Benzene.Elements.Core;

public class ElementsRegistrations : RegistrationsBase
{
    public ElementsRegistrations()
    {
        Add(".AddBroadcastEvent()", x => x.AddBroadcastEvent());
    }
}
