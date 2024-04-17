using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

public class InlineHealthCheck : IHealthCheck
{
    private readonly Func<Task<IHealthCheckResult>> _func;

    public InlineHealthCheck(Func<Task<IHealthCheckResult>> func)
        : this(string.Empty, func)
    {
        _func = func;
    }

    public InlineHealthCheck(string type, Func<Task<IHealthCheckResult>> func)
    {
        Type = type;
        _func = func;
    }

    public string Type { get; }

    public Task<IHealthCheckResult> ExecuteAsync()
    {
        return _func();
    }
}
