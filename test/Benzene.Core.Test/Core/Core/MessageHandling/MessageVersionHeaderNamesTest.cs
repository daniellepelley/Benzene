using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class MessageVersionHeaderNamesTest
{
    private class TestContext
    {
    }

    private class FixedHeadersGetter : IMessageHeadersGetter<TestContext>
    {
        private readonly IDictionary<string, string> _headers;
        public FixedHeadersGetter(IDictionary<string, string> headers) => _headers = headers;
        public IDictionary<string, string> GetHeaders(TestContext context) => _headers;
    }

    private static IMessageVersionGetter<TestContext> BuildVersionGetter(
        IDictionary<string, string> headers, string[]? overrideHeaderNames)
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddScoped<IMessageHeadersGetter<TestContext>>(_ => new FixedHeadersGetter(headers));
        container.AddHeaderMessageVersionGetter<TestContext>();
        if (overrideHeaderNames != null)
        {
            container.AddMessageVersionHeaderNames(overrideHeaderNames);
        }

        var resolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        return resolver.GetService<IMessageVersionGetter<TestContext>>();
    }

    [Fact]
    public void AddHeaderMessageVersionGetter_NoOverride_UsesDefaultHeaderNames()
    {
        var getter = BuildVersionGetter(
            new Dictionary<string, string> { ["benzene-version"] = "V2", ["schema-version"] = "V9" },
            overrideHeaderNames: null);

        Assert.Equal("V2", getter.GetVersion(new TestContext()));
    }

    [Fact]
    public void AddMessageVersionHeaderNames_OverrideRegistered_VersionGetterReadsConfiguredHeader()
    {
        var getter = BuildVersionGetter(
            new Dictionary<string, string> { ["benzene-version"] = "V2", ["schema-version"] = "V9" },
            overrideHeaderNames: ["schema-version"]);

        // The app-wide override replaces the default fallback, so the default "benzene-version"
        // header is ignored and the configured "schema-version" wins.
        Assert.Equal("V9", getter.GetVersion(new TestContext()));
    }
}
