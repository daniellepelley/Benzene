using Benzene.Core.DI;

namespace Benzene.Aws.Lambda.ApiGateway;

public class ApiGatewayRegistrations : RegistrationsBase
{
    public ApiGatewayRegistrations()
    {
        Add(".AddApiGateway()", x => x.AddApiGateway());
    }
}
