using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using Benzene.Abstractions.Logging;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.Logging;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage.TestHelpers;
using Benzene.Diagnostics.Correlation;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Tools.Aws;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Aws;

public class AwsEventStreamLogContextTest
{
    private AwsLambdaBenzeneTestHost _host;
    private Mock<IBenzeneLogContext> _mockBenzeneContext;

    private void SetUp(Action<ILogContextBuilder<AwsEventStreamContext>> action)
    {
        _mockBenzeneContext = new Mock<IBenzeneLogContext>();
        _host = new InlineAwsLambdaStartUp()
            .ConfigureServices(x => x.AddScoped(_ => _mockBenzeneContext.Object).UsingBenzene(b => b.AddBenzene()))
            .Configure(app => app
                .UseLogResult(action)
            )
            .BuildHost();
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
        
        await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

        VerifyExists("correlationId");
    }
    
    [Fact]
    public async Task LambdaContext()
    {
        SetUp(x => x
            .WithApplication()
            .WithRequestId()
        );
        
        await _host.SendEventAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()), new TestLambdaContext
        {
            FunctionName = "foo::bar",
            AwsRequestId = "some-id"
        });

        Verify("application", "foo");
        Verify("requestId", "some-id");
    }

    [Fact]
    public async Task LambdaContext_Empty()
    {
        SetUp(x => x
            .WithApplication()
            .WithRequestId()
        );
        
        await _host.SendEventAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()), new TestLambdaContext());

        VerifyDoesNotExists("application");
        VerifyDoesNotExists("requestId");
    }
}
