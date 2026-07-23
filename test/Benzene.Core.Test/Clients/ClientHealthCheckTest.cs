using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Results;
using Benzene.Clients.HealthChecks;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Clients;

public class ClientHealthCheckTest
{
    private const string ServiceName = "OrderService";

    // A response as the generated client's HealthCheckAsync() produces it: fetched from the provider,
    // then drift-annotated locally by ClientHealthCheckProcessor (schema check -> Warning + ClientHashMatch).
    private static HealthCheckResponse AnnotatedResponse(string serviceHash, string clientHash)
    {
        var data = new Dictionary<string, object> { [SchemaHealthCheckConstants.HashCodeKey] = serviceHash };
        var schema = (HealthCheckResult)HealthCheckResult.CreateInstance(true, SchemaHealthCheckConstants.Type, data);
        var response = new HealthCheckResponse(true,
            new Dictionary<string, HealthCheckResult> { [SchemaHealthCheckConstants.Type] = schema });
        return (HealthCheckResponse)ClientHealthCheckProcessor.Process(response, clientHash);
    }

    private class FakeClient : IHasHealthCheck
    {
        private readonly Func<Task<IBenzeneResult<HealthCheckResponse>>> _call;
        public FakeClient(Func<Task<IBenzeneResult<HealthCheckResponse>>> call) => _call = call;
        public string HashCode => "client-hash";
        public Task<IBenzeneResult<HealthCheckResponse>> HealthCheckAsync() => _call();
    }

    private static ClientHealthCheck CheckReturning(IBenzeneResult<HealthCheckResponse> result) =>
        new(ServiceName, new FakeClient(() => Task.FromResult(result)));

    [Fact]
    public async Task Reachable_ContractsMatch_ReportsOk()
    {
        var result = await CheckReturning(BenzeneResult.Ok(AnnotatedResponse("same", "same"))).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal(ServiceName, result.Type);
        Assert.Equal(true, result.Data["reachable"]);
    }

    [Fact]
    public async Task Reachable_ContractDrift_ReportsWarning()
    {
        var result = await CheckReturning(BenzeneResult.Ok(AnnotatedResponse("service", "client"))).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Warning, result.Status);
        var match = Assert.IsType<ClientHashMatch>(result.Data[SchemaHealthCheckConstants.MatchKey]);
        Assert.False(match.IsMatch);
        Assert.Equal("service", match.ServiceHashCode);
    }

    [Fact]
    public async Task Reachable_ContractDrift_DoesNotFlipAggregateIsHealthy()
    {
        // The core degraded-but-not-fatal guarantee: drift is a Warning, and a Warning must not make
        // the aggregate response unhealthy (it must not gate traffic even on the diagnostic topic).
        var check = CheckReturning(BenzeneResult.Ok(AnnotatedResponse("service", "client")));

        var aggregate = await new HealthCheckProcessor().PerformHealthChecksAsync(new IHealthCheck[] { check });

        Assert.True(aggregate.IsSuccessful);
        Assert.Equal(BenzeneResultStatus.Ok, aggregate.Status);
        Assert.True(((HealthCheckResponse)aggregate.PayloadAsObject).IsHealthy);
    }

    [Fact]
    public async Task Reachable_NoSchemaCheckToCompare_ReportsOkNotFalseWarning()
    {
        var response = new HealthCheckResponse(true, new Dictionary<string, HealthCheckResult>());

        var result = await CheckReturning(BenzeneResult.Ok(response)).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.False(result.Data.ContainsKey(SchemaHealthCheckConstants.MatchKey));
    }

    [Fact]
    public async Task Unreachable_NullPayload_ReportsFailed()
    {
        var result = await CheckReturning(
            BenzeneResult.Set<HealthCheckResponse>("service-unavailable", false)).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal(false, result.Data["reachable"]);
        Assert.Equal("service-unavailable", result.Data["status"]);
    }

    [Fact]
    public async Task ClientThrows_ReportsFailedNotThrows()
    {
        var check = new ClientHealthCheck(ServiceName,
            new FakeClient(() => throw new InvalidOperationException("connection refused")));

        var result = await check.ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal(false, result.Data["reachable"]);
        Assert.Equal("connection refused", result.Data["error"]);
    }

    [Fact]
    public async Task ReportsServiceDependencyMetadata()
    {
        var result = await CheckReturning(BenzeneResult.Ok(AnnotatedResponse("same", "same"))).ExecuteAsync();

        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Service", dependency.Kind);
        Assert.Equal(ServiceName, dependency.Name);
    }
}
