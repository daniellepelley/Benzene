using Benzene.Core.DI;

namespace Benzene.Aws.Sqs;

public class SqsRegistrations : RegistrationsBase
{
    public SqsRegistrations()
    {
        Add(".AddSqs()", x => x.AddSqs());
    }
}
