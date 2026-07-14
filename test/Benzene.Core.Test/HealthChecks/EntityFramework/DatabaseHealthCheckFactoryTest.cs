using Benzene.Abstractions.DI;
using Benzene.HealthChecks.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Benzene.Test.HealthChecks.EntityFramework;

public class DatabaseHealthCheckFactoryTest
{
    [Fact]
    public void Create_ResolvesDbContextFromResolver()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(nameof(Create_ResolvesDbContextFromResolver))
            .Options;
        using var context = new TestDbContext(options);

        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<TestDbContext>()).Returns(context);

        var factory = new DatabaseHealthCheckFactory<TestDbContext>("20260101000000_Initial");
        var healthCheck = factory.Create(mockResolver.Object);

        Assert.IsType<DatabaseHealthCheck<TestDbContext>>(healthCheck);
        Assert.Equal("Database", healthCheck.Type);
    }
}
