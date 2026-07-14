using System.Collections.Generic;
using Benzene.HealthChecks.Core;
using Xunit;

namespace Benzene.Test.Plugins.HealthChecks;

public class HealthCheckResultTest
{
    [Fact]
    public void CreateInstance_WithoutDependencies_DefaultsToEmpty()
    {
        var result = HealthCheckResult.CreateInstance(true, "some-type", new Dictionary<string, object>());

        Assert.Empty(result.Dependencies);
    }

    [Fact]
    public void CreateInstance_WithDependencies_PopulatesThem()
    {
        var dependencies = new[] { new HealthCheckDependency("Queue", "some-queue-url") };

        var result = HealthCheckResult.CreateInstance(true, "some-type", new Dictionary<string, object>(), dependencies);

        Assert.Same(dependencies, result.Dependencies);
    }

    [Fact]
    public void CreateWarning_WithDependencies_PopulatesThem()
    {
        var dependencies = new[] { new HealthCheckDependency("Database", "OrdersDbContext") };

        var result = HealthCheckResult.CreateWarning("some-type", new Dictionary<string, object>(), dependencies);

        Assert.Equal(HealthCheckStatus.Warning, result.Status);
        Assert.Same(dependencies, result.Dependencies);
    }

    [Fact]
    public void IHealthCheckResult_DefaultInterfaceMember_ReturnsEmptyWhenNotOverridden()
    {
        IHealthCheckResult result = new MinimalHealthCheckResult();

        Assert.Empty(result.Dependencies);
    }

    // Deliberately does not override Dependencies, to prove IHealthCheckResult's default interface
    // member keeps this source/binary compatible with implementers written before Dependencies existed.
    private class MinimalHealthCheckResult : IHealthCheckResult
    {
        public string Status => HealthCheckStatus.Ok;
        public string Type => "minimal";
        public IDictionary<string, object> Data => new Dictionary<string, object>();
    }
}
