using System;
using System.Collections.Generic;
using Benzene.HealthChecks.Core;
using Xunit;

namespace Benzene.Test.HealthChecks;

/// <summary>
/// Coverage for the shared failure-classification policy (§3.4 / §3.9): permission errors degrade to
/// Warning, everything else fails, the non-sensitive discriminators are surfaced, and the exception
/// message never is.
/// </summary>
public class HealthCheckErrorTest
{
    private static readonly HealthCheckDependency[] Deps = { new("Queue", "orders") };

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public void PermissionStatus_DegradesToWarning(int status)
    {
        var result = HealthCheckError.Classify("Sqs", new Exception(), Deps, "AuthorizationError", status);

        Assert.Equal(HealthCheckStatus.Warning, result.Status);
        Assert.Equal("AuthorizationError", result.Data["ErrorCode"]);
        Assert.Equal(status, result.Data["StatusCode"]);
        Assert.Equal("orders", Assert.Single(result.Dependencies).Name);
    }

    [Theory]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(503)]
    public void NonPermissionStatus_Fails(int status)
    {
        var result = HealthCheckError.Classify("Sqs", new Exception(), Deps, "InternalFailure", status);

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
    }

    [Fact]
    public void NoStatus_Fails_AndOmitsTheDiscriminators()
    {
        // A raw connectivity failure (not an SDK service exception) has no code/status to report.
        var result = HealthCheckError.Classify("Sqs", new InvalidOperationException(), Deps);

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("InvalidOperationException", result.Data["Error"]);
        Assert.False(result.Data.ContainsKey("ErrorCode"));
        Assert.False(result.Data.ContainsKey("StatusCode"));
    }

    [Fact]
    public void NeverLeaksTheExceptionMessage()
    {
        var result = HealthCheckError.Classify("Sqs",
            new Exception("host=db.internal;password=hunter2"), Deps, "AccessDenied", 403);

        foreach (var value in result.Data.Values)
        {
            Assert.DoesNotContain("hunter2", value.ToString());
        }
    }

    [Fact]
    public void PreservesCallerSuppliedData()
    {
        var data = new Dictionary<string, object> { { "TopicArn", "arn:aws:sns:eu-west-1:1:orders" } };

        var result = HealthCheckError.Classify("Sns", new Exception(), Deps, "AccessDenied", 403, data);

        Assert.Equal("arn:aws:sns:eu-west-1:1:orders", result.Data["TopicArn"]);
        Assert.Equal(HealthCheckStatus.Warning, result.Status);
    }
}
