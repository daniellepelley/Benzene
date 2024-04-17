using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

internal class TimeOutHealthCheck : IHealthCheck
{
    private readonly IHealthCheck _inner;
    public string Type => _inner.Type;

    public TimeOutHealthCheck(IHealthCheck inner)
    {
        _inner = inner;
    }

    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var task = _inner.ExecuteAsync();
        await Task.WhenAny(Task.Delay(10000), task);

        if (task.IsCompleted)
        {
            return task.Result;
        }

        return HealthCheckResult.CreateInstance(false, _inner.Type, new Dictionary<string, object>
            {
                { "Error", "Timed Out"}
            });
    }
}
