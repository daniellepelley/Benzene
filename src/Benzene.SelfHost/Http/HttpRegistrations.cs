using Benzene.Core.DI;

namespace Benzene.SelfHost.Http;

public class HttpRegistrations : RegistrationsBase
{
    public HttpRegistrations()
    {
        Add(".AddHttp()", x => x.AddHttp());
    }
}
