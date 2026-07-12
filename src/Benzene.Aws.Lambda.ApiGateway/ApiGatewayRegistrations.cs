using Benzene.Core.DI;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Declares the dependency injection registrations made by <see cref="DependencyInjectionExtensions.AddApiGateway"/>,
/// for use by <see cref="RegistrationCheck"/>'s missing-registration diagnostics.
/// </summary>
public class ApiGatewayRegistrations : RegistrationsBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayRegistrations"/> class.
    /// </summary>
    public ApiGatewayRegistrations()
    {
        Add(".AddApiGateway()", x => x.AddApiGateway());
    }
}
