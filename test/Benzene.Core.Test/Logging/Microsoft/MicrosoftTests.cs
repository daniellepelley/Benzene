using System;
using System.Linq;
using Benzene.Abstractions.Logging;
using Benzene.Microsoft.Dependencies;
using Benzene.Microsoft.Logging;
using Benzene.Test.Logging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Benzene.Test.Logging.Microsoft;

public class MicrosoftTests
{

    [Theory]
    [InlineData(BenzeneLogLevel.Trace, LogLevel.Trace)]
    [InlineData(BenzeneLogLevel.Debug, LogLevel.Debug)]
    [InlineData(BenzeneLogLevel.Information, LogLevel.Information)]
    [InlineData(BenzeneLogLevel.Warning, LogLevel.Warning)]
    [InlineData(BenzeneLogLevel.Error, LogLevel.Error)]
    [InlineData(BenzeneLogLevel.Critical, LogLevel.Critical)]
    [InlineData(BenzeneLogLevel.None, LogLevel.None)]
    public void LogTest(BenzeneLogLevel benzeneLogLevel, LogLevel logLevel)
    {
        var testLogger = new TestLogger();

        var logger = new MicrosoftBenzeneLogAppender(testLogger);
        logger.Log(benzeneLogLevel, new Exception(), "log", "value");

        Assert.Equal(logLevel, testLogger.Logs.First());
    }

    [Fact]
    public void DependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.UsingBenzene(x => x.AddMicrosoftLogger());

        var resolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var logger = resolver.GetService<IBenzeneLogAppender>();
        Assert.IsType<MicrosoftBenzeneLogAppender>(logger);
    }
}
