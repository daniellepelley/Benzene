using Benzene.Core.DI;

namespace Benzene.Aws.Core;

public class AwsRegistrations : RegistrationsBase
{
    public AwsRegistrations()
    {
        Add(".AddAwsMessageHandlers(<assemblies>)", x => x.AddMessageHandlers());
    }
}
