using Benzene.Core.DI;

namespace Benzene.Aws.ApiGateway;

public class ApiGatewayRegistrations : RegistrationsBase
{
    public ApiGatewayRegistrations()
    {
        Add(".AddApiGateway()", x => x.AddApiGateway());
    }
}
