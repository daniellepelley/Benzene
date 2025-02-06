using Benzene.Core.DI;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Lambda.Core;

public class AwsRegistrations : RegistrationsBase
{
    public AwsRegistrations()
    {
        Add(".AddMessageHandlers(<assemblies>)", x => x.AddMessageHandlers());
    }
}
