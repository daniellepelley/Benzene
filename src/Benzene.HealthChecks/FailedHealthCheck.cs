using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

public class FailedHealthCheck : IHealthCheck
{
    private readonly Exception _exception;

    public FailedHealthCheck(Exception exception)
    {
        _exception = exception;
    }
    
    public string Type => "Failed";

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        return Task.FromResult(HealthCheckResult.CreateInstance(false, Type, new Dictionary<string, object>
        {
            {"Exception", _exception.Message }
        }));
    }
}
