using Autofac;
using Benzene.Autofac;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Benzene.Test.Logging;

public class DefaultLoggingResolutionTest
{
    [Fact]
    public void MicrosoftDependencies_NoHostLoggingConfigured_LoggerStillResolves()
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene());

        var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();

        Assert.NotNull(scope.GetService<ILoggerFactory>());
        Assert.NotNull(scope.GetService<ILogger<DefaultLoggingResolutionTest>>());
    }

    [Fact]
    public void MicrosoftDependencies_HostLoggingConfiguredAfterUsingBenzene_HostConfigurationWins()
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene());
        services.AddLogging(x => x.SetMinimumLevel(LogLevel.Warning));

        var factory = new MicrosoftServiceResolverFactory(services);
        using var scope = factory.CreateScope();

        var logger = scope.GetService<ILogger<DefaultLoggingResolutionTest>>();
        Assert.NotNull(logger);
        Assert.False(logger.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void Autofac_NoLoggingRegistered_LoggerStillResolves()
    {
        var containerBuilder = new ContainerBuilder();
        var factory = new AutofacServiceResolverFactory(containerBuilder);
        using var scope = factory.CreateScope();

        Assert.NotNull(scope.GetService<ILoggerFactory>());
        Assert.NotNull(scope.GetService<ILogger<DefaultLoggingResolutionTest>>());
    }

    [Fact]
    public void Autofac_UserRegistrationWins()
    {
        var containerBuilder = new ContainerBuilder();
        using var loggerFactory = LoggerFactory.Create(x => x.SetMinimumLevel(LogLevel.Warning));
        containerBuilder.RegisterInstance(loggerFactory).As<ILoggerFactory>();

        var factory = new AutofacServiceResolverFactory(containerBuilder);
        using var scope = factory.CreateScope();

        Assert.Same(loggerFactory, scope.GetService<ILoggerFactory>());
        Assert.False(scope.GetService<ILogger<DefaultLoggingResolutionTest>>().IsEnabled(LogLevel.Information));
    }
}
