using Benzene.Abstractions.Results;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.HealthChecks
{
    public interface IHasHealthCheck
    {
        string HashCode { get; }
        Task<IBenzeneResult<HealthCheckResponse>> HealthCheckAsync();
    }
}
