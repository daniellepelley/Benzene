using Benzene.Core.DI;

namespace Benzene.Aws.Sns;

public class SnsRegistrations : RegistrationsBase
{
    public SnsRegistrations()
    {
        Add(".AddSns()", x => x.AddSns());
    }
}
