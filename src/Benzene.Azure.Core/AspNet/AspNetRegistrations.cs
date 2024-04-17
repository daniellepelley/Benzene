using Benzene.Core.DI;

namespace Benzene.Azure.Core.AspNet;

public class AspNetRegistrations : RegistrationsBase
{
    public AspNetRegistrations()
    {
        Add(".AddAspNet()", x => x.AddAspNet());
    }
}
