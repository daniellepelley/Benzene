using Benzene.Core.DI;

namespace Benzene.SelfHost.Http;

public class HttpListenerRegistrations : RegistrationsBase
{
    public HttpListenerRegistrations()
    {
        Add(".AddHttp()", x => x.AddHttp());
    }
}
