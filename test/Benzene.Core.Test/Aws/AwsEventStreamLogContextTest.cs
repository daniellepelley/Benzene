using System;
using System.Threading.Tasks;
using Benzene.Abstractions.Logging;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.MessageHandlers.TestHelpers;
using Benzene.Core.MessageHandlers.DI;
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

namespace Benzene.Test.Aws;

public class AwsEventStreamLogContextTest
{
    private AwsLambdaBenzeneTestHost _host;
    private FakeLoggerFactory _fakeLoggerFactory;

    private void SetUp(Action<ILogContextBuilder<AwsEventStreamContext>> action)
    {
        _fakeLoggerFactory = new FakeLoggerFactory();
        _host = new InlineAwsLambdaStartUp()
            .ConfigureServices(x => x
                .AddSingleton<ILoggerFactory>(_ => _fakeLoggerFactory)
                .UsingBenzene(b => b.AddBenzene()))
            .Configure(app => app
                .UseLogResult(action)
            )
            .BuildHost();
    }

    private void VerifyExists(string key)
    {
        Assert.Contains(_fakeLoggerFactory.Collector.ScopeDictionaries, x => x.ContainsKey(key));
    }

    private void VerifyDoesNotExists(string key)
    {
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

}
