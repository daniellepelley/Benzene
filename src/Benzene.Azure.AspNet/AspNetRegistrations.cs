using Benzene.Core.DI;

namespace Benzene.Azure.AspNet;

public class AspNetRegistrations : RegistrationsBase
{
    public AspNetRegistrations()
    {
        Add(".AddAspNet()", x => x.AddAspNet());
    }
}
