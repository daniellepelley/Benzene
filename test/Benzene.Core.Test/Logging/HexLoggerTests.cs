using System;
using Benzene.Abstractions.Logging;
using Moq;
using Xunit;

namespace Benzene.Test.Logging;

public class BenzeneLoggerTests
{
    [Fact]
    public void Information()
    {
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogInformation("foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Information, null, "foo"));
    }

    [Fact]
    public void Information_Exception()
    {
        var exception = new Exception();
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogInformation(exception, "foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Information, exception, "foo"));
    }

    [Fact]
    public void Trace()
    {
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogTrace("foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Trace, null, "foo"));
    }

    [Fact]
    public void Trace_Exception()
    {
        var exception = new Exception();
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogTrace(exception, "foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Trace, exception, "foo"));
    }

    [Fact]
    public void Debug()
    {
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogDebug("foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Debug, null, "foo"));
    }

    [Fact]
    public void Debug_Exception()
    {
        var exception = new Exception();
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogDebug(exception, "foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Debug, exception, "foo"));
    }

    [Fact]
    public void Warning()
    {
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogWarning("foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Warning, null, "foo"));
    }

    [Fact]
    public void Warning_Exception()
    {
        var exception = new Exception();
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogWarning(exception, "foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Warning, exception, "foo"));
    }

    [Fact]
    public void Error()
    {
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogError("foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Error, null, "foo"));
    }

    [Fact]
    public void Error_Exception()
    {
        var exception = new Exception();
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogError(exception, "foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Error, exception, "foo"));
    }

    [Fact]
    public void Critical()
    {
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogCritical("foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Critical, null, "foo"));
    }
    
    [Fact]
    public void Critical_Exception()
    {
        var exception = new Exception();
        var benzeneLogger = new Mock<IBenzeneLogger>();
        benzeneLogger.Object.LogCritical(exception, "foo");
        benzeneLogger.Verify(x => x.Log(BenzeneLogLevel.Critical, exception, "foo"));
    }
}
