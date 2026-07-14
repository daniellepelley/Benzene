using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// An <see cref="IHealthCheck"/> whose result is produced by an arbitrary delegate, allowing a check to
/// be defined inline (e.g. via <see cref="HealthCheckBuilderExtensions"/>) without writing a dedicated
/// class.
/// </summary>
public class InlineHealthCheck : IHealthCheck
{
    private readonly Func<Task<IHealthCheckResult>> _func;

    /// <summary>Initializes a new instance of the <see cref="InlineHealthCheck"/> class with an empty <see cref="Type"/>.</summary>
    /// <param name="func">Produces the check's result when <see cref="ExecuteAsync"/> is called.</param>
    public InlineHealthCheck(Func<Task<IHealthCheckResult>> func)
        : this(string.Empty, func)
    {
        _func = func;
    }

    /// <summary>Initializes a new instance of the <see cref="InlineHealthCheck"/> class.</summary>
    /// <param name="type">The check's identifier, used as its key in the aggregated response.</param>
    /// <param name="func">Produces the check's result when <see cref="ExecuteAsync"/> is called.</param>
    public InlineHealthCheck(string type, Func<Task<IHealthCheckResult>> func)
    {
        Type = type;
        _func = func;
    }

    /// <inheritdoc />
    public string Type { get; }

    /// <summary>Invokes the delegate supplied at construction and returns its result.</summary>
    public Task<IHealthCheckResult> ExecuteAsync()
    {
        return _func();
    }
}
