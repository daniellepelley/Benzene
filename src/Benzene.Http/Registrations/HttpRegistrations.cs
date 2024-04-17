using Benzene.Core.DI;

namespace Benzene.Http.Registrations;

public class HttpRegistrations : RegistrationsBase
{
    public HttpRegistrations()
    {
        Add(".AddHttpMessageHandlers()", x => x.AddHttpMessageHandlers());
    }
}
