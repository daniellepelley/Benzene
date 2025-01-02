using Benzene.Core.DI;

namespace Benzene.Aws.Lambda.Sqs;

public class SqsRegistrations : RegistrationsBase
{
    public SqsRegistrations()
    {
        Add(".AddSqs()", x => x.AddSqs());
    }
}
