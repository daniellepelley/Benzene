using Benzene.HealthChecks.Core;

namespace Benzene.Clients.HealthChecks
{
    public class ClientHealthCheckProcessor
    {
        public static IHealthCheckResponse<HealthCheckResult> Process(IHealthCheckResponse<HealthCheckResult> healthCheckResponse, string hashCode)
        {
            var schemaHealthCheck = healthCheckResponse.HealthChecks.FirstOrDefault(x => x.Value.Type == "schema").Value;
            dynamic data = schemaHealthCheck.Data["data"];

            var serviceHashCode = data.hashCode.ToString();
            var isMatch = hashCode == serviceHashCode;
            var status = isMatch ? "ok" : "warning";

            schemaHealthCheck.Data["data"] = new ClientHashMatch
            {
                ServiceHashCode = serviceHashCode,
                ClientHashCode = hashCode,
                IsMatch = isMatch
            };

            return new HealthCheckResponse(
                healthCheckResponse.IsHealthy,
                healthCheckResponse.HealthChecks
            );
        }
    }
}
