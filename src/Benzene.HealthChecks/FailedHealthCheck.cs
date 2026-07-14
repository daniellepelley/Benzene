using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// A placeholder <see cref="IHealthCheck"/> that always reports a failed result carrying the message of
/// a pre-existing <see cref="Exception"/>. Used by <see cref="Extensions.BuildHealthCheck"/> to represent a
/// health check that could not even be constructed.
/// </summary>
public class FailedHealthCheck : IHealthCheck
{
    private readonly Exception _exception;

    /// <summary>Initializes a new instance of the <see cref="FailedHealthCheck"/> class.</summary>
    /// <param name="exception">The exception whose message is reported as the failure reason.</param>
    public FailedHealthCheck(Exception exception)
    {
        _exception = exception;
    }

    /// <inheritdoc />
    public string Type => "Failed";

    /// <summary>Always returns a failed result whose <c>Data</c> contains the wrapped exception's message under the <c>"Exception"</c> key.</summary>
    public Task<IHealthCheckResult> ExecuteAsync()
    {
        return Task.FromResult(HealthCheckResult.CreateInstance(false, Type, new Dictionary<string, object>
        {
            {"Exception", _exception.Message }
        }));
    }
}
