using Benzene.Abstractions.DI;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using IHealthCheckFactory = Benzene.HealthChecks.Core.IHealthCheckFactory;

namespace Benzene.Test.Plugins.HealthChecks;

public class SimpleHealthCheckFactory : IHealthCheckFactory
{
    public IHealthCheck Create(IServiceResolver resolver)
    {
        return new SimpleHealthCheck();
    }
}


