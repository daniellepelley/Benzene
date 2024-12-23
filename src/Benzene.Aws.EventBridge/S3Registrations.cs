using Benzene.Core.DI;

namespace Benzene.Aws.EventBridge;

public class S3Registrations : RegistrationsBase
{
    public S3Registrations()
    {
        Add(".AddS3()", x => x.AddS3());
    }
}
