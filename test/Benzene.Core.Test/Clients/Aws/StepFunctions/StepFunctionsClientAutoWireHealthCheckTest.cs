using System;
using System.Linq;
using Amazon.StepFunctions;
using Benzene.Abstractions.DI;
using Benzene.Clients.Aws.StepFunctions;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Clients.Aws.StepFunctions;

/// <summary>
/// Coverage for Phase 4 Step Functions auto-wiring: the new <c>AddStepFunctionsClient(arn)</c> DI seam
/// auto-registers the non-destructive reachability check on the dependency category (deep
/// <c>healthcheck</c> layer only), deduped by the state machine ARN, with a <c>healthCheck: false</c> opt-out.
/// </summary>
public class StepFunctionsClientAutoWireHealthCheckTest
{
    private const string Arn = "arn:aws:states:eu-west-1:123:stateMachine:orders";

    private sealed class TestRegister : IRegisterDependency
    {
        private readonly IServiceCollection _services;
        public TestRegister(IServiceCollection services) => _services = services;
        public void Register(Action<IBenzeneServiceContainer> action) => action(new MicrosoftBenzeneServiceContainer(_services));
    }

    private static IHealthCheckFinder Finder(Action<IBenzeneServiceContainer> register)
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddScoped(_ => Mock.Of<IAmazonStepFunctions>());
        _ = new HealthCheckBuilder(new TestRegister(services));
        register(container);

        var factory = new MicrosoftServiceResolverFactory(services);
        var scope = factory.CreateScope();
        return scope.GetService<IHealthCheckFinder>();
    }

    [Fact]
    public void AddStepFunctionsClient_AutoRegistersADependencyCheck_OnTheDependencyCategoryOnly()
    {
        var finder = Finder(c => c.AddStepFunctionsClient(Arn));

        Assert.Single(finder.FindDependencyHealthChecks(), x => x.Type == "StepFunctions");
        Assert.DoesNotContain(finder.FindHealthChecks(), x => x.Type == "StepFunctions");
    }

    [Fact]
    public void AddStepFunctionsClient_HealthCheckFalse_RegistersNoDependencyCheck()
    {
        var finder = Finder(c => c.AddStepFunctionsClient(Arn, healthCheck: false));

        Assert.DoesNotContain(finder.FindDependencyHealthChecks(), x => x.Type == "StepFunctions");
    }

    [Fact]
    public void AddStepFunctionsClient_SameArnTwice_CollapsesToOneCheck()
    {
        var finder = Finder(c =>
        {
            c.AddStepFunctionsClient(Arn);
            c.AddStepFunctionsClient(Arn);
        });

        Assert.Single(finder.FindDependencyHealthChecks());
    }
}
