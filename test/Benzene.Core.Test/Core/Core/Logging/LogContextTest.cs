using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Logging;
using Benzene.Aws.Core;
using Benzene.Aws.Core.DirectMessage;
using Benzene.Core.DirectMessage;
using Benzene.Core.Logging;
using Benzene.Test.Examples;
using Benzene.Tools;
using Benzene.Tools.Aws;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.Logging;

public class LogContextTest
{
    private AwsLambdaBenzeneTestHost _host;
    private Mock<IBenzeneLogContext> _mockBenzeneContext;

    private void SetUp(Action<LogContextBuilder<DirectMessageContext>> action)
    {
        _mockBenzeneContext = new Mock<IBenzeneLogContext>();
        _host = new InlineAwsLambdaStartUp()
            .ConfigureServices(x => x
                .AddScoped(_ => _mockBenzeneContext.Object))
            .Configure(app => app
                .UseDirectMessage(direct => direct
                    .UseLogResult(action))
            ).BuildHost();
    }

    private void VerifyExists(string key)
    {
        _mockBenzeneContext.Verify(logContext => logContext.Create(It.Is<IDictionary<string, string>>(
                x => x.ContainsKey(key)
            )));
    }

    private void VerifyDoesNotExists(string key)
    {
        _mockBenzeneContext.Verify(logContext => logContext.Create(It.Is<IDictionary<string, string>>(
                x => !x.ContainsKey(key)
            )));
    }
    
    private void Verify(string key, string value)
    {
        _mockBenzeneContext.Verify(logContext => logContext.Create(It.Is<IDictionary<string, string>>(
                x => x.ContainsKey(key) && x[key] == value
            )));
    }
    
    [Fact]
    public async Task CorrelationId()
    {
        SetUp(x => x.WithCorrelationId());
        
        await _host.SendDirectMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        VerifyExists("correlationId");
    }

    [Fact]
    public async Task Transport()
    {
        SetUp(x => x.WithTransport());

        await _host.SendDirectMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        Verify("transport", "direct");
    }

    [Fact]
    public async Task Topic()
    {
        SetUp(x => x.WithTopic());

        await _host.SendDirectMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        Verify("topic", Defaults.Topic);
    }


    [Fact]
    public async Task Headers()
    {
        SetUp(x => x.WithHeaders("sender"));

        await _host.SendDirectMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()).WithHeader("sender", "foo"));

        VerifyExists("sender");
    }

    [Fact]
    public async Task Headers_NullValue()
    {
        SetUp(x => x.WithHeaders("sender"));

        await _host.SendDirectMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()).WithHeader("sender", string.Empty));

        VerifyDoesNotExists("sender");
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

        await _host.SendDirectMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        Verify("key1", "value1");
        Verify("key2", "value2");
        Verify("key3", "value3");
        Verify("key4", "value4");
        Verify("key5", "value5");
        Verify("key6", "value6");
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

        await _host.SendDirectMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        Verify("key1", "value1");
        Verify("key2", "value2");
        Verify("key3", "value3");
        Verify("key4", "value4");
        Verify("key5", "value5");
        Verify("key6", "value6");

    }
}
