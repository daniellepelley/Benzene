using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Benzene.HealthChecks.Core;
using Benzene.HealthChecks.DynamoDb;
using Moq;
using Xunit;

namespace Benzene.Test.HealthChecks.DynamoDb;

public class DynamoDbHealthCheckTest
{
    [Fact]
    public async Task ExecuteAsync_TableActive_ReturnsHealthy()
    {
        var mock = new Mock<IAmazonDynamoDB>();
        mock.Setup(x => x.DescribeTableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DescribeTableResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Table = new TableDescription { TableStatus = TableStatus.ACTIVE }
            });

        var result = await new DynamoDbHealthCheck("orders", mock.Object).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Ok, result.Status);
        Assert.Equal("DynamoDb", result.Type);
        var dependency = Assert.Single(result.Dependencies);
        Assert.Equal("Table", dependency.Kind);
        Assert.Equal("orders", dependency.Name);
    }

    [Fact]
    public async Task ExecuteAsync_ClientThrows_ReturnsUnhealthy_WithTheTableDependency()
    {
        var mock = new Mock<IAmazonDynamoDB>();
        mock.Setup(x => x.DescribeTableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResourceNotFoundException("no such table"));

        var result = await new DynamoDbHealthCheck("orders", mock.Object).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.Equal("ResourceNotFoundException", result.Data["Error"]);
        Assert.Equal("orders", Assert.Single(result.Dependencies).Name);
    }
}
