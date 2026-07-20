using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;
using Benzene.HealthChecks.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Benzene.Test.HealthChecks.EntityFramework;

public class DatabaseHealthCheckExtensionsTest
{
    private static (Mock<IHealthCheckBuilder> Builder, TestDbContext Context) Setup(string dbName)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(dbName).Options;
        var context = new TestDbContext(options);
        var builder = new Mock<IHealthCheckBuilder>();
        builder.Setup(x => x.AddHealthCheck(It.IsAny<System.Func<IServiceResolver, IHealthCheck>>())).Returns(builder.Object);
        return (builder, context);
    }

    [Fact]
    public void AddDatabaseHealthCheck_RegistersADatabaseHealthCheck()
    {
        var (builder, context) = Setup(nameof(AddDatabaseHealthCheck_RegistersADatabaseHealthCheck));
        System.Func<IServiceResolver, IHealthCheck> captured = null;
        builder.Setup(x => x.AddHealthCheck(It.IsAny<System.Func<IServiceResolver, IHealthCheck>>()))
            .Callback<System.Func<IServiceResolver, IHealthCheck>>(f => captured = f).Returns(builder.Object);

        builder.Object.AddDatabaseHealthCheck<TestDbContext>("20260101000000_Initial");

        var resolver = new Mock<IServiceResolver>();
        resolver.Setup(x => x.GetService<TestDbContext>()).Returns(context);
        Assert.IsType<DatabaseHealthCheck<TestDbContext>>(captured(resolver.Object));
    }

    [Fact]
    public void AddDatabaseConnectionHealthCheck_RegistersAConnectionHealthCheck()
    {
        var (builder, context) = Setup(nameof(AddDatabaseConnectionHealthCheck_RegistersAConnectionHealthCheck));
        System.Func<IServiceResolver, IHealthCheck> captured = null;
        builder.Setup(x => x.AddHealthCheck(It.IsAny<System.Func<IServiceResolver, IHealthCheck>>()))
            .Callback<System.Func<IServiceResolver, IHealthCheck>>(f => captured = f).Returns(builder.Object);

        builder.Object.AddDatabaseConnectionHealthCheck<TestDbContext>();

        var resolver = new Mock<IServiceResolver>();
        resolver.Setup(x => x.GetService<TestDbContext>()).Returns(context);
        Assert.IsType<DatabaseConnectionHealthCheck<TestDbContext>>(captured(resolver.Object));
    }
}
