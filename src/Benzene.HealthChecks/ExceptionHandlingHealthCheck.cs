using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// Decorates an <see cref="IHealthCheck"/> so that an exception thrown out of <see cref="ExecuteAsync"/>
/// is caught and turned into a failed <see cref="IHealthCheckResult"/> (with the exception message in
/// its <c>Data</c>) instead of propagating and aborting the whole health check run. Used internally by
/// <see cref="HealthCheckProcessor"/> to wrap every check.
/// </summary>
internal class ExceptionHandlingHealthCheck : IHealthCheck
{
    private readonly IHealthCheck _inner;

    /// <inheritdoc />
    public string Type => _inner.Type;

    /// <summary>Initializes a new instance of the <see cref="ExceptionHandlingHealthCheck"/> class.</summary>
    /// <param name="inner">The health check to run and guard against exceptions.</param>
    public ExceptionHandlingHealthCheck(IHealthCheck inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Runs the wrapped check. If it throws, returns a failed result containing the exception's message
    /// instead of letting the exception propagate.
    /// </summary>
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
