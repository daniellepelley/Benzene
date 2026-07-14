namespace Benzene.HealthChecks.Core;

/// <summary>Describes one external dependency an <see cref="IHealthCheck"/> verifies connectivity to.</summary>
public class HealthCheckDependency
{
    /// <summary>Initializes a new instance of the <see cref="HealthCheckDependency"/> class.</summary>
    /// <param name="kind">The category of dependency, e.g. <c>"Queue"</c>, <c>"Database"</c>, <c>"Http"</c>, <c>"Lambda"</c>, <c>"StateMachine"</c>, <c>"Cache"</c>.</param>
    /// <param name="name">The specific resource identifier, e.g. a queue URL, a DbContext type name, an endpoint URL, or a function name. Never a connection string or other secret.</param>
    public HealthCheckDependency(string kind, string name)
    {
        Kind = kind;
        Name = name;
    }

    /// <summary>The category of dependency, e.g. <c>"Queue"</c>, <c>"Database"</c>, <c>"Http"</c>, <c>"Lambda"</c>, <c>"StateMachine"</c>, <c>"Cache"</c>. An open string rather than an enum, so new dependency kinds don't require coordinating a shared type.</summary>
    public string Kind { get; }

    /// <summary>The specific resource identifier, e.g. a queue URL, a DbContext type name, an endpoint URL, or a function name.</summary>
    public string Name { get; }
}
