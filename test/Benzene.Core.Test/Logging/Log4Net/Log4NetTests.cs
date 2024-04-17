using System;
using System.Linq;
using Benzene.Abstractions.Logging;
using Benzene.Core.Logging;
using Benzene.Log4Net;
using Benzene.Microsoft.Dependencies;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Logging.Log4Net;

public class Log4NetTests
{
    private readonly MemoryAppender _appender;

    public Log4NetTests()
    {
        var config = "<log4net><root><level value=\"ALL\"/></root></log4net>";
        var stream = Tools.Utils.StringToStream(config);

        log4net.Config.XmlConfigurator.Configure(stream);
        _appender = new MemoryAppender { Threshold = Level.All };
        var logger = (Logger)LogManager.GetLogger(typeof(BenzeneLogger)).Logger;
        logger.AddAppender(_appender);
    }

    [Theory]
    [InlineData(BenzeneLogLevel.Trace, "DEBUG")]
    [InlineData(BenzeneLogLevel.Debug, "DEBUG")]
    [InlineData(BenzeneLogLevel.Information, "INFO")]
    [InlineData(BenzeneLogLevel.Warning, "WARN")]
    [InlineData(BenzeneLogLevel.Error, "ERROR")]
    [InlineData(BenzeneLogLevel.Critical, "FATAL")]
    public void LogTest(BenzeneLogLevel benzeneLogLevel, string expectedLogLevel)
    {
        var appender1 = new Log4NetBenzeneLogAppender();
        appender1.Log(benzeneLogLevel, new Exception(), "log", "value");

        Assert.Equal(expectedLogLevel, _appender.GetEvents().First().Level.Name);
    }

    [Fact]
    public void DependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.UsingBenzene(x => x.AddLog4Net());

        var resolver = new MicrosoftServiceResolverFactory(services).CreateScope();

        var logger = resolver.GetService<IBenzeneLogAppender>();
        Assert.IsType<Log4NetBenzeneLogAppender>(logger);
    }
}
