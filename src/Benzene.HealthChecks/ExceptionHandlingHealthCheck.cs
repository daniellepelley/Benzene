using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

internal class ExceptionHandlingHealthCheck : IHealthCheck
{
    private readonly IHealthCheck _inner;
    public string Type => _inner.Type;

    public ExceptionHandlingHealthCheck(IHealthCheck inner)
    {
        _inner = inner;
    }

    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        try
        {
            return await _inner.ExecuteAsync();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.CreateInstance(false, _inner.Type, new Dictionary<string, object>
            {
                { "Exception", ex.Message }
            });
        }
    }
}
