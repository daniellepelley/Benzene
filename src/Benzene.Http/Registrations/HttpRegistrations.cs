using Benzene.Core.DI;

namespace Benzene.Http.Registrations;

/// <summary>
/// Provides automatic registration of HTTP services for Benzene applications.
/// </summary>
/// <remarks>
/// This class extends <see cref="RegistrationsBase"/> to automatically register HTTP-related
/// services when the application discovers and loads Benzene packages. It ensures that HTTP
/// message handlers, routing, status code mapping, and header mapping services are available
/// in the dependency injection container.
/// </remarks>
public class HttpRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRegistrations"/> class and registers HTTP services.
    /// </summary>
    public HttpRegistrations()
    {
        Add(".AddHttpMessageHandlers()", x => x.AddHttpMessageHandlers());
    }
}
