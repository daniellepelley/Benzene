using Benzene.Core.DI;

namespace Benzene.Aws.EventBridge;

public class SnsRegistrations : RegistrationsBase
{
    public SnsRegistrations()
    {
        Add(".AddS3()", x => x.AddS3());
    }
}
