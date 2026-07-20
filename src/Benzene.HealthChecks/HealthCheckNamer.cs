using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// Assigns unique keys to health check results for the aggregated response dictionary, so that multiple
/// checks with the same (or an empty) <see cref="IHealthCheck.Type"/> don't collide. Not thread-safe - a
/// new instance is created per health check run (see <see cref="HealthCheckProcessor"/>).
/// </summary>
internal class HealthCheckNamer
{
    private readonly Dictionary<string, int> _existingNames = new() {{ "HealthCheck", 0 }};

    /// <summary>
    /// Returns a unique name for a health check result. An empty or null <paramref name="name"/> is
    /// treated as <c>"HealthCheck"</c>. Because <c>"HealthCheck"</c> is pre-seeded as already used, the
    /// first check with an empty type is returned as <c>"HealthCheck-1"</c> rather than bare
    /// <c>"HealthCheck"</c>; subsequent collisions with any name are suffixed <c>-2</c>, <c>-3</c>, etc.
    /// </summary>
    /// <param name="name">The candidate name (typically the health check's <see cref="IHealthCheck.Type"/>).</param>
    /// <returns>A name guaranteed to be unique among all names returned by this instance so far.</returns>
    public string GetName(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            return ReturnName(name);
        }
        return ReturnName("HealthCheck");
    }

    /// <summary>
    /// Returns <paramref name="name"/> unchanged the first time it is seen, otherwise appends an
    /// incrementing suffix (e.g. <c>"name-2"</c>, <c>"name-3"</c>) to keep it unique.
    /// </summary>
    /// <param name="name">The candidate name.</param>
    /// <returns>A name guaranteed to be unique among all names returned by this instance so far.</returns>
    public string ReturnName(string name)
    {
        if (_existingNames.TryAdd(name, 1))
        {
            return name;
        }

        // Reserve the generated "name-N" too, and keep bumping until the candidate is genuinely
        // unused. Previously the suffixed name was returned but never recorded, so a later check whose
        // Type literally equalled a generated name (e.g. checks typed "a","a","a-2") produced two
        // identical keys and HealthCheckProcessor's ToDictionary threw - taking down the whole probe
        // (all checks, incl. liveness/readiness) with a 500 instead of returning the report.
        string candidate;
        do
        {
            _existingNames[name]++;
            candidate = $"{name}-{_existingNames[name]}";
        } while (!_existingNames.TryAdd(candidate, 1));

        return candidate;
    }
}
