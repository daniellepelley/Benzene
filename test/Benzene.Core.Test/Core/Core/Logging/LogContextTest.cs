using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.Logging;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.TestHelpers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Diagnostics.Correlation;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Benzene.Test.Logging.Helpers;
using Benzene.Testing;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Benzene.Test.Core.Core.Logging;

public class LogContextTest
{
    private AwsLambdaBenzeneTestHost _host;
    private FakeLoggerFactory _fakeLoggerFactory;

    private void SetUp(Action<ILogContextBuilder<BenzeneMessageContext>> action)
    {
        _fakeLoggerFactory = new FakeLoggerFactory();
        _host = new InlineAwsLambdaStartUp()
            .ConfigureServices(x => x
                .UsingBenzene(b => b.AddBenzene())
                .AddSingleton<ILoggerFactory>(_ => _fakeLoggerFactory))
            .Configure(app => app
                .UseBenzeneMessage(direct => direct
                    .UseLogResult(action))
            ).BuildHost();
    }

    private void VerifyExists(string key)
    {
        Assert.Contains(_fakeLoggerFactory.Collector.ScopeDictionaries, x => x.ContainsKey(key));
    }

    private void VerifyDoesNotExists(string key)
    {
        Assert.Contains(_fakeLoggerFactory.Collector.ScopeDictionaries, x => !x.ContainsKey(key));
        Assert.DoesNotContain(_fakeLoggerFactory.Collector.ScopeDictionaries, x => x.ContainsKey(key));
    }

    private void Verify(string key, string value)
    {
        Assert.Contains(_fakeLoggerFactory.Collector.ScopeDictionaries,
            x => x.ContainsKey(key) && x[key]?.ToString() == value);
    }

    [Fact]
    public async Task CorrelationId()
    {
        SetUp(x => x.WithCorrelationId());

        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        VerifyExists("correlationId");
    }

    [Fact]
    public async Task Transport()
    {
        SetUp(x => x.WithTransport());

        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        Verify("transport", "benzene");
    }

    [Fact]
    public async Task Topic()
    {
        SetUp(x => x.WithTopic());

        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        Verify("topic", Defaults.Topic);
    }


    [Fact]
    public async Task Headers()
    {
        SetUp(x => x.WithHeaders("sender"));

        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()).WithHeader("sender", "foo"));

        VerifyExists("sender");
    }

    [Fact]
    public async Task Headers_NullValue()
    {
        SetUp(x => x.WithHeaders("sender"));

        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()).WithHeader("sender", string.Empty));

        VerifyDoesNotExists("sender");
    }

    [Fact]
    public async Task ProcessTime()
    {
        SetUp(x => x.WithTopic());

        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        VerifyExists("processTime");
        var benzeneResultEntry = Assert.Single(_fakeLoggerFactory.Collector.Entries,
            x => x.Message == "BenzeneResult");
        Assert.Equal(LogLevel.Information, benzeneResultEntry.Level);
    }

    [Fact]
    public async Task OnRequest()
    {
        SetUp(x => x
            .OnRequest("key1", "value1")
            .OnRequest("key2", _ => "value2")
            .OnRequest("key3", (_, _) => "value3")
            .OnRequest(new Dictionary<string, string>{{"key4", "value4"}})
            .OnRequest(_ => new Dictionary<string, string>{{"key5", "value5"}})
            .OnRequest((_,_) => new Dictionary<string, string>{{"key6", "value6"}})
        );

        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        Verify("key1", "value1");
        Verify("key2", "value2");
        Verify("key3", "value3");
        Verify("key4", "value4");
        Verify("key5", "value5");
        Verify("key6", "value6");
    }

    [Fact]
    public async Task OnRequest_DuplicateKey_LastWins_WithoutThrowing()
    {
        // Two extractors emitting the same key must not crash the log-scope build (ToDictionary threw);
        // the later value wins.
        SetUp(x => x
            .OnRequest("dup", "first")
            .OnRequest("dup", "second"));

        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        Verify("dup", "second");
    }

    [Fact]
    public async Task OnResponse()
    {
        SetUp(x => x
            .OnResponse("key1", "value1")
            .OnResponse("key2", _ => "value2")
            .OnResponse("key3", (_, _) => "value3")
            .OnResponse(new Dictionary<string, string> { { "key4", "value4" } })
            .OnResponse(_ => new Dictionary<string, string> { { "key5", "value5" } })
            .OnResponse((_, _) => new Dictionary<string, string> { { "key6", "value6" } })
        );

        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        Verify("key1", "value1");
        Verify("key2", "value2");
        Verify("key3", "value3");
        Verify("key4", "value4");
        Verify("key5", "value5");
        Verify("key6", "value6");

    }
}
