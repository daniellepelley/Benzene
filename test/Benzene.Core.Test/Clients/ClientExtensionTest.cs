using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.SQS;
using Benzene.Abstractions;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Clients;
using Benzene.Clients.Aws;
using Benzene.Clients.Aws.Lambda;
using Benzene.Clients.Aws.Sqs;
using Benzene.Clients.CorrelationId;
using Benzene.Core.Middleware;
using Benzene.Results;
using Benzene.Test.Clients.Aws.Samples;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Void = Benzene.Results.Void;

namespace Benzene.Test.Clients;
//Named clients
//Filter by topic
//Decorators

public class ClientExtensionTest
{
    private Mock<IAmazonLambda> _mockAmazonLambda;
    private ServiceCollection _services;

    private void SetUp(Action<IBenzeneServiceContainer> action = null)
    {
        _services = new ServiceCollection();
        _mockAmazonLambda = new Mock<IAmazonLambda>();
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        var mockCorrelationId = new Mock<ICorrelationId>();

        _mockAmazonLambda.Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvokeResponse());

        mockCorrelationId.Setup(x => x.Get())
            .Returns("foo");

        _services.AddScoped(_ => _mockAmazonLambda.Object);
        _services.AddScoped(_ => mockAmazonSqs.Object);
        _services.AddScoped(_ => mockCorrelationId.Object);
        _services.AddScoped(_ => Mock.Of<IBenzeneLogger>());

        var benzene = new Microsoft.Dependencies.MicrosoftBenzeneServiceContainer(_services);

        if (action == null)
        {
            benzene.AddBenzeneMessageClients(x => x
                .CreateSqsBenzeneMessageClient("sqs", "some-queue", new NullServiceResolver(), sqs => sqs.WithCorrelationId())
                .CreateAwsLambdaBenzeneMessageClient("lambda2", map => map.ForService("lambda1"))
                .CreateAwsLambdaBenzeneMessageClient("sns", map => map.ForTopic("topic1"))
                .CreateAwsLambdaBenzeneMessageClient("lambdai", map => map.ForServiceAndTopic("lambda3", "topic2"), lambda =>
                    lambda
                        .WithCorrelationId()
                        .WithRetry(2)
                )
            );
        }
        else
        {
            action(benzene);
        }
    }

    // [Fact]
    // public async Task Test()
    // {
    //     SetUp(benzene =>
    //         benzene.AddBenzeneMessageClients(x => x
    //             .CreateAwsLambdaBenzeneMessageClient("lambda", map => map.ForServiceAndTopic("lambda3", "topic2"), lambda =>
    //                 lambda
    //                     .WithCorrelationId()
    //                     .WithRetry(2))));
    //
    //     var serviceResolver = new Microsoft.Dependencies.MicrosoftServiceResolverAdapter(_services.BuildServiceProvider());
    //
    //     var client = serviceResolver.GetService<IBenzeneMessageClient>();
    //     Assert.IsType<RetryBenzeneMessageClient>(client);
    //
    //     var benzeneResult = await client.SendMessageAsync<ExamplePayload, Void>(Defaults.Topic, new ExamplePayload());
    //     Assert.True(benzeneResult.IsAccepted());
    //
    //     _mockAmazonLambda.Verify(x => x.InvokeAsync(It.Is<InvokeRequest>(request =>
    //         JsonConvert.DeserializeObject<BenzeneMessageClientRequest>(request.Payload).Headers["correlationId"] == "foo"
    //     ), It.IsAny<CancellationToken>()));
    // }

    [Fact]
    public async Task TestSqsByTopic()
    {
        SetUp();
        var serviceResolver = new Microsoft.Dependencies.MicrosoftServiceResolverAdapter(_services.BuildServiceProvider());

        var factory = serviceResolver.GetService<IBenzeneMessageClientFactory>();
        var client = factory.Create("", "topic1");

        Assert.IsType<AwsLambdaBenzeneMessageClient>(client);

        var result = await client.SendMessageAsync<ExamplePayload, Void>(Defaults.Topic, new ExamplePayload());
        Assert.True(result.IsAccepted());

        _mockAmazonLambda.Verify(x => x.InvokeAsync(It.Is<InvokeRequest>(request =>
            !JsonConvert.DeserializeObject<BenzeneMessageClientRequest>(request.Payload).Headers.Any()
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task TestSqsForServiceAndTopic()
    {
        SetUp();
        var serviceResolver = new Microsoft.Dependencies.MicrosoftServiceResolverAdapter(_services.BuildServiceProvider());

        var factory = serviceResolver.GetService<IBenzeneMessageClientFactory>();
        var client = factory.Create("lambda3", "topic1");

        Assert.IsType<AwsLambdaBenzeneMessageClient>(client);

        var result = await client.SendMessageAsync<ExamplePayload, Void>(Defaults.Topic, new ExamplePayload());
        Assert.True(result.IsAccepted());

        _mockAmazonLambda.Verify(x => x.InvokeAsync(It.Is<InvokeRequest>(request =>
            !JsonConvert.DeserializeObject<BenzeneMessageClientRequest>(request.Payload).Headers.Any()
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task TestSqs()
    {
        SetUp();
        var serviceResolver = new Microsoft.Dependencies.MicrosoftServiceResolverAdapter(_services.BuildServiceProvider());

        var factory = serviceResolver.GetService<IBenzeneMessageClientFactory>();
        var client = factory.Create("lambda1", "");

        Assert.IsType<AwsLambdaBenzeneMessageClient>(client);

        var result = await client.SendMessageAsync<ExamplePayload, Void>(Defaults.Topic, new ExamplePayload());
        Assert.True(result.IsAccepted());

        _mockAmazonLambda.Verify(x => x.InvokeAsync(It.Is<InvokeRequest>(request =>
            !JsonConvert.DeserializeObject<BenzeneMessageClientRequest>(request.Payload).Headers.Any()
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public void NoDuplicates_For_Single_Client_Test()
    {
        SetUp();
        var benzene = new Microsoft.Dependencies.MicrosoftBenzeneServiceContainer(_services);

        Assert.Throws<ArgumentException>(() =>
            benzene.AddBenzeneMessageClients(x => x
                .CreateAwsLambdaBenzeneMessageClient("lambda2", map => map.ForService("lambda1").ForService("lambda1"))
            )
        );

    }

    [Fact]
    public void NoDuplicates_Multiple_Clients_Test()
    {
        SetUp();
        var benzene = new Microsoft.Dependencies.MicrosoftBenzeneServiceContainer(_services);

        Assert.Throws<ArgumentException>(() =>
            benzene.AddBenzeneMessageClients(x => x
                .CreateAwsLambdaBenzeneMessageClient("lambda2", map => map.ForService("lambda1"))
                .CreateAwsLambdaBenzeneMessageClient("lambda2", map => map.ForService("lambda1"))
            )
        );

    }
    
}
