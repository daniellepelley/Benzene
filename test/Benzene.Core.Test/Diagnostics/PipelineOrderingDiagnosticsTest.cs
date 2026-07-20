using System.Linq;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Logging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Benzene.Test.Diagnostics;

public class PipelineOrderingDiagnosticsTest
{
    private static (MiddlewarePipelineBuilder<object> Builder, MicrosoftServiceResolverFactory Factory) NewBuilder()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var builder = new MiddlewarePipelineBuilder<object>(container);
        return (builder, new MicrosoftServiceResolverFactory(services));
    }

    [Fact]
    public void W3CTraceContextNotFirst_IsFlagged()
    {
        var (builder, factory) = NewBuilder();
        builder.Use("first", (_, next) => next());
        builder.UseW3CTraceContext();

        using var resolver = factory.CreateScope();
        var issues = resolver.FindPipelineOrderingIssues(builder);

        var issue = Assert.Single(issues);
        Assert.Equal("W3CTraceContext", issue.MiddlewareName);
        Assert.Equal(1, issue.Index);
    }

    [Fact]
    public void W3CTraceContextFirst_IsNotFlagged()
    {
        var (builder, factory) = NewBuilder();
        builder.UseW3CTraceContext();
        builder.Use("second", (_, next) => next());

        using var resolver = factory.CreateScope();
        var issues = resolver.FindPipelineOrderingIssues(builder);

        Assert.Empty(issues);
    }

    [Fact]
    public void NoW3CTraceContext_ProducesNoIssues()
    {
        var (builder, factory) = NewBuilder();
        builder.Use("first", (_, next) => next());
        builder.Use("second", (_, next) => next());

        using var resolver = factory.CreateScope();
        var issues = resolver.FindPipelineOrderingIssues(builder);

        Assert.Empty(issues);
    }

    [Fact]
    public void LogPipelineOrderingIssues_WarnsWhenMisordered()
    {
        var (builder, factory) = NewBuilder();
        builder.Use("first", (_, next) => next());
        builder.UseW3CTraceContext();

        var collector = new FakeLogCollector();
        var logger = new FakeLogger(collector, "test");

        using var resolver = factory.CreateScope();
        var issues = resolver.LogPipelineOrderingIssues(builder, logger);

        Assert.Single(issues);
        Assert.Contains(collector.Entries, e =>
            e.Level == LogLevel.Warning && e.Message.Contains("W3CTraceContext"));
    }
}
