using Benzene.Core.DI;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Core;

public class AwsRegistrations : RegistrationsBase
{
    public AwsRegistrations()
    {
        Add(".AddAwsMessageHandlers(<assemblies>)", x => x.AddMessageHandlers());
    }
}
