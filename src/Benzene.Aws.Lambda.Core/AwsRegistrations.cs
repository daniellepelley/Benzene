using Benzene.Core.DI;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;

namespace Benzene.Aws.Lambda.Core;

public class AwsRegistrations : RegistrationsBase
{
    public AwsRegistrations()
    {
        Add(".AddMessageHandlers(<assemblies>)", x => x.AddMessageHandlers());
    }
}
