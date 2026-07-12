using Amazon.Lambda;
using Amazon.SQS;
using Amazon.StepFunctions;
using Benzene.Abstractions.DI;
using Benzene.Clients;
using Benzene.Clients.Aws;
using Benzene.Clients.Aws.Lambda;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Client;

public class ExtensionsTest
{
    [Fact]
    public void AddBenzeneMessageClients_ReturnsSameContainer()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        var result = container.AddBenzeneMessageClients(x => { });

        Assert.Same(container, result);
    }

    [Fact]
    public void AddBenzeneMessageClient_ReturnsSameContainer()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);

        var result = container.AddBenzeneMessageClient(x => x.WithMessageClient(_ => Mock.Of<IBenzeneMessageClient>()));

        Assert.Same(container, result);
    }

    [Fact]
    public void AddLambdaClients_ReturnsSameContainer()
    {
        // NOTE: AddLambdaClients registers AwsLambdaBenzeneMessageClient and
        // AwsLambdaBenzeneMessageClientFactory via bare AddScoped<T>(), but both require a
        // constructor `string lambdaName` that this method never supplies to the container.
        // Resolving IBenzeneMessageClient/IBenzeneMessageClientFactory/AwsLambdaBenzeneMessageClient
        // via DI therefore throws - this is a pre-existing gap in the production code, not covered
        // here. IAwsLambdaClient/IClientHeaders are the only services from this method that don't
        // need a lambda name and are safe to resolve.
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IAmazonLambda>());
        var container = new MicrosoftBenzeneServiceContainer(services);

        var result = container.AddLambdaClients("some-sender");

        Assert.Same(container, result);

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IAwsLambdaClient>());
        Assert.NotNull(provider.GetService<IClientHeaders>());
    }

    [Fact]
    public void AddSqsHealthCheck_RegistersHealthCheckWithSqsType()
    {
        var mockBuilder = new Mock<IHealthCheckBuilder>();
        System.Func<IServiceResolver, IHealthCheck> factory = null;
        mockBuilder.Setup(x => x.AddHealthCheck(It.IsAny<System.Func<IServiceResolver, IHealthCheck>>()))
            .Callback<System.Func<IServiceResolver, IHealthCheck>>(f => factory = f)
            .Returns(mockBuilder.Object);

        var result = mockBuilder.Object.AddSqsHealthCheck("some-queue-url");

        Assert.Same(mockBuilder.Object, result);
        Assert.NotNull(factory);

        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<IAmazonSQS>()).Returns(Mock.Of<IAmazonSQS>());

        var healthCheck = factory(mockResolver.Object);

        Assert.Equal("Sqs", healthCheck.Type);
    }

    [Fact]
    public void AddLambdaHealthCheck_RegistersHealthCheckWithLambdaType()
    {
        var mockBuilder = new Mock<IHealthCheckBuilder>();
        System.Func<IServiceResolver, IHealthCheck> factory = null;
        mockBuilder.Setup(x => x.AddHealthCheck(It.IsAny<System.Func<IServiceResolver, IHealthCheck>>()))
            .Callback<System.Func<IServiceResolver, IHealthCheck>>(f => factory = f)
            .Returns(mockBuilder.Object);

        var result = mockBuilder.Object.AddLambdaHealthCheck("some-lambda");

        Assert.Same(mockBuilder.Object, result);
        Assert.NotNull(factory);

        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<IAmazonLambda>()).Returns(Mock.Of<IAmazonLambda>());
        mockResolver.Setup(x => x.GetService<ILogger<AwsLambdaHealthCheck>>()).Returns(NullLogger<AwsLambdaHealthCheck>.Instance);

        var healthCheck = factory(mockResolver.Object);

        Assert.Equal("Lambda", healthCheck.Type);
    }

    [Fact]
    public void AddStepFunctionHealthCheck_RegistersHealthCheckWithStepFunctionsType()
    {
        var mockBuilder = new Mock<IHealthCheckBuilder>();
        System.Func<IServiceResolver, IHealthCheck> factory = null;
        mockBuilder.Setup(x => x.AddHealthCheck(It.IsAny<System.Func<IServiceResolver, IHealthCheck>>()))
            .Callback<System.Func<IServiceResolver, IHealthCheck>>(f => factory = f)
            .Returns(mockBuilder.Object);

        var result = mockBuilder.Object.AddStepFunctionHealthCheck("some-state-machine-arn");

        Assert.Same(mockBuilder.Object, result);
        Assert.NotNull(factory);

        var mockResolver = new Mock<IServiceResolver>();
        mockResolver.Setup(x => x.GetService<IAmazonStepFunctions>()).Returns(Mock.Of<IAmazonStepFunctions>());

        var healthCheck = factory(mockResolver.Object);

        Assert.Equal("StepFunctions", healthCheck.Type);
    }
}
