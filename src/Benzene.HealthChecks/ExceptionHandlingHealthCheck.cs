using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// Decorates an <see cref="IHealthCheck"/> so that an exception thrown out of <see cref="ExecuteAsync"/>
/// is caught and turned into a failed <see cref="IHealthCheckResult"/> (with the exception's type name
/// in its <c>Data</c>) instead of propagating and aborting the whole health check run. Used internally
/// by <see cref="HealthCheckProcessor"/> to wrap every check.
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
    /// Runs the wrapped check. If it throws, returns a failed result containing the exception's type
    /// name instead of letting the exception propagate. Deliberately reports the type name, not the
    /// message - exception messages can carry sensitive details (e.g. connection strings for some
    /// ADO.NET providers), and this result can flow out to whatever calls the health check topic with
    /// no built-in authorization.
    /// </summary>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        try
        {
            return await _inner.ExecuteAsync();
        }
        catch (OperationCanceledException)
        {
            // Cancellation (ambient token / shutdown) is not a dependency failure - report it as a
            // distinct "Cancelled" outcome rather than an opaque "OperationCanceledException" so
            // operators aren't misled into treating a graceful shutdown as a broken dependency.
            return HealthCheckResult.CreateInstance(false, _inner.Type, new Dictionary<string, object>
            {
                { "Error", "Cancelled" }
            });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.CreateInstance(false, _inner.Type, new Dictionary<string, object>
            {
                { "Exception", ex.GetType().Name }
            });
        }
    }
}
