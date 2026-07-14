using System.Net.Http;
using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;
using Benzene.HealthChecks.Http;
using Moq;
using Xunit;

namespace Benzene.Test.HealthChecks.Http;

public class HttpPingHealthCheckFactoryTest
{
    [Fact]
    public void Create_ResolvesHttpClientFromResolver()
    {
        var httpClient = new HttpClient();
        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<HttpClient>()).Returns(httpClient);

        var factory = new HttpPingHealthCheckFactory("https://example.test/ping");
        var healthCheck = factory.Create(mockResolver.Object);

        Assert.IsType<HttpPingHealthCheck>(healthCheck);
        Assert.Equal("HttpPing", healthCheck.Type);
    }

    [Fact]
    public void AddHttpPing_RegistersFactory()
    {
        var mockBuilder = new Mock<IHealthCheckBuilder>();
        mockBuilder.Setup(x => x.AddHealthCheck(It.IsAny<System.Func<IServiceResolver, IHealthCheck>>()))
            .Returns(mockBuilder.Object);

        var result = mockBuilder.Object.AddHttpPing("https://example.test/ping");

        Assert.Same(mockBuilder.Object, result);
        mockBuilder.Verify(x => x.AddHealthCheck(It.IsAny<System.Func<IServiceResolver, IHealthCheck>>()), Times.Once);
    }
}
