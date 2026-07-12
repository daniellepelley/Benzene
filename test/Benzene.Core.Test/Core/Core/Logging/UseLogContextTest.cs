using System;
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
using Benzene.Tools.Aws;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Benzene.Test.Core.Core.Logging;

public class UseLogContextTest
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
                    .UseLogContext(action))
            ).BuildHost();
    }

    private void Verify(string key, string value)
    {
        Assert.Contains(_fakeLoggerFactory.Collector.ScopeDictionaries,
            x => x.ContainsKey(key) && x[key]?.ToString() == value);
    }

    [Fact]
    public async Task CorrelationId_AddedToLogContextForDurationOfRequest()
    {
        SetUp(x => x.WithCorrelationId());

        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        Assert.Contains(_fakeLoggerFactory.Collector.ScopeDictionaries, x => x.ContainsKey("correlationId"));
    }

    [Fact]
    public async Task OnRequest_AddsConfiguredKeyToLogContext()
    {
        SetUp(x => x.OnRequest("key1", "value1"));

        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        Verify("key1", "value1");
    }
}
