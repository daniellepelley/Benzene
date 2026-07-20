using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Microsoft.Dependencies;
using Benzene.ResponseEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.ResponseEvents;

public class ResponseEventDiagnosticsTest
{
    private class OrderPayload
    {
    }

    private class OrderHandler
    {
    }

    private class NotifyHandler
    {
    }

    private class FakeHandlersFinder : IMessageHandlersFinder
    {
        private readonly IMessageHandlerDefinition[] _definitions;
        public FakeHandlersFinder(params IMessageHandlerDefinition[] definitions) => _definitions = definitions;
        public IMessageHandlerDefinition[] FindDefinitions() => _definitions;
    }

    private static Benzene.Abstractions.DI.IServiceResolver BuildResolver(
        IEnumerable<IMessageHandlerDefinition> definitions,
        ResponseEventMappings mappings = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandlersFinder>(new FakeHandlersFinder(definitions.ToArray()));
        if (mappings != null)
        {
            services.AddSingleton<IResponseEventCatalog>(
                new ResponseEventCatalog(new[] { mappings }, Array.Empty<ResponseEventDeclarations>()));
        }

        return new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
    }

    private static ResponseEventMappings Mappings(params IResponseEventMapping[] mappings) =>
        new ResponseEventMappings(mappings, PublishFailureMode.FailMessage);

    [Fact]
    public void FindUnmappedResponseHandlers_ResponseHandlerWithNoMapping_IsReported()
    {
        var resolver = BuildResolver(new[]
        {
            MessageHandlerDefinition.CreateInstance("order:cancel", typeof(OrderPayload), typeof(OrderPayload), typeof(OrderHandler)),
        });

        var gaps = resolver.FindUnmappedResponseHandlers();

        var gap = Assert.Single(gaps);
        Assert.Equal("order:cancel", gap.Topic.Id);
        Assert.Equal(typeof(OrderHandler), gap.HandlerType);
        Assert.Equal(typeof(OrderPayload), gap.ResponseType);
    }

    [Fact]
    public void FindUnmappedResponseHandlers_NoResponseHandler_IsIgnored()
    {
        var resolver = BuildResolver(new[]
        {
            MessageHandlerDefinition.CreateInstance("order:notify", typeof(OrderPayload), typeof(Void), typeof(NotifyHandler)),
        });

        Assert.Empty(resolver.FindUnmappedResponseHandlers());
    }

    [Fact]
    public void FindUnmappedResponseHandlers_ExplicitMappingCoversTopic_IsIgnored()
    {
        var resolver = BuildResolver(
            new[]
            {
                MessageHandlerDefinition.CreateInstance("order:create", typeof(OrderPayload), typeof(OrderPayload), typeof(OrderHandler)),
            },
            Mappings(new ExplicitResponseEventMapping("order:create", "order:created")));

        Assert.Empty(resolver.FindUnmappedResponseHandlers());
    }

    [Fact]
    public void FindUnmappedResponseHandlers_CrudConventionCoversCreateButNotCancel()
    {
        var resolver = BuildResolver(
            new[]
            {
                MessageHandlerDefinition.CreateInstance("order:create", typeof(OrderPayload), typeof(OrderPayload), typeof(OrderHandler)),
                MessageHandlerDefinition.CreateInstance("order:cancel", typeof(OrderPayload), typeof(OrderPayload), typeof(OrderHandler)),
            },
            Mappings(new CrudConventionResponseEventMapping()));

        var gap = Assert.Single(resolver.FindUnmappedResponseHandlers());
        Assert.Equal("order:cancel", gap.Topic.Id);
    }

    [Fact]
    public void FindUnmappedResponseHandlers_NoCatalogRegistered_ReportsAllResponseHandlers()
    {
        var resolver = BuildResolver(new[]
        {
            MessageHandlerDefinition.CreateInstance("order:create", typeof(OrderPayload), typeof(OrderPayload), typeof(OrderHandler)),
        });

        Assert.Single(resolver.FindUnmappedResponseHandlers());
    }

    [Fact]
    public void FindUnmappedResponseHandlers_NoFinderRegistered_ReturnsEmpty()
    {
        var resolver = new MicrosoftServiceResolverAdapter(new ServiceCollection().BuildServiceProvider());

        Assert.Empty(resolver.FindUnmappedResponseHandlers());
    }

    [Fact]
    public void FindUnmappedResponseHandlers_OrdersByTopicThenHandler()
    {
        var resolver = BuildResolver(new[]
        {
            MessageHandlerDefinition.CreateInstance("b:cancel", typeof(OrderPayload), typeof(OrderPayload), typeof(OrderHandler)),
            MessageHandlerDefinition.CreateInstance("a:cancel", typeof(OrderPayload), typeof(OrderPayload), typeof(OrderHandler)),
        });

        var gaps = resolver.FindUnmappedResponseHandlers();

        Assert.Equal(new[] { "a:cancel", "b:cancel" }, gaps.Select(x => x.Topic.Id));
    }

    [Fact]
    public void LogUnmappedResponseHandlers_LogsAWarningPerGapAndReturnsThem()
    {
        var resolver = BuildResolver(new[]
        {
            MessageHandlerDefinition.CreateInstance("order:cancel", typeof(OrderPayload), typeof(OrderPayload), typeof(OrderHandler)),
        });
        var logger = new CapturingLogger();

        var gaps = resolver.LogUnmappedResponseHandlers(logger);

        Assert.Single(gaps);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("order:cancel", entry.Message);
    }

    [Fact]
    public void CrudConvention_Covers_CreateUpdateDeleteOnly()
    {
        var mapping = new CrudConventionResponseEventMapping();

        Assert.True(mapping.Covers(new Topic("order:create")));
        Assert.True(mapping.Covers(new Topic("order:update")));
        Assert.True(mapping.Covers(new Topic("order:delete")));
        Assert.False(mapping.Covers(new Topic("order:cancel")));
    }

    [Fact]
    public void ExplicitMapping_Covers_MatchesSourceTopicCaseInsensitively()
    {
        IResponseEventMapping mapping = new ExplicitResponseEventMapping("order:create", "order:created");

        Assert.True(mapping.Covers(new Topic("ORDER:create")));
        Assert.False(mapping.Covers(new Topic("order:cancel")));
    }

    private class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
