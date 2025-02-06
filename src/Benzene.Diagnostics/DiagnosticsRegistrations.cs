using Benzene.Core.DI;
using Benzene.Diagnostics.Correlation;

namespace Benzene.Diagnostics;

public class DiagnosticsRegistrations : RegistrationsBase
{
    public DiagnosticsRegistrations()
    {
        Add(".AddCorrelationId()", x => x.AddCorrelationId());
    }
}
