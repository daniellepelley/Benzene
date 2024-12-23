using Benzene.HealthChecks.Core;
using Benzene.Results;

namespace Benzene.Clients.HealthChecks
{
    public interface IHasHealthCheck
    {
        string HashCode { get; }
        Task<IBenzeneResult<HealthCheckResponse>> HealthCheckAsync();
    }
}
