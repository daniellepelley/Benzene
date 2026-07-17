using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.FluentValidation;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Plugins.FluentValidation;

public class EnhancedFluentValidationTest
{
    public class SampleRequest
    {
        public string Name { get; set; }
    }

    public class SampleValidator : AbstractValidator<SampleRequest>
    {
        public SampleValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithStatus(BenzeneResultStatus.BadRequest);
        }
    }

    public class SampleHandler : IMessageHandler<SampleRequest, string>
    {
        public Task<IBenzeneResult<string>> HandleAsync(SampleRequest request)
        {
            return Task.FromResult(BenzeneResult.Ok("Success"));
        }
    }

    [Fact]
    public async Task ValidationWithStatusFromFluentValidation()
    {
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());
        serviceCollection.AddTransient<SampleHandler>();
        serviceCollection.AddTransient<IValidator<SampleRequest>, SampleValidator>();

        var container = new MicrosoftBenzeneServiceContainer(serviceCollection);
        container.AddFluentValidation(new[] { typeof(SampleValidator) });

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        pipeline.UseMessageHandlers(x => x
            .UseFluentValidation()
            .AddMessageHandler<SampleHandler, SampleRequest, string>("test")
        );

        var app = new BenzeneMessageApplication(pipeline.Build());
        var request = new BenzeneMessageRequest
        {
            Topic = "test",
            Body = "{\"Name\":\"\"}"
        };

        var response = await app.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));

        Assert.Equal(BenzeneResultStatus.BadRequest, response.StatusCode);
    }

    public class HandlerLevelValidator : AbstractValidator<SampleRequest>
    {
        public HandlerLevelValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    [Fact]
    public async Task ValidationWithNoExplicitStatus_UsesIDefaultStatusesValidationError()
    {
        // No per-rule WithStatus and no per-handler override: the framework-wide default from
        // IDefaultStatuses governs, not a handler-specific value.
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());
        serviceCollection.AddTransient<SampleHandler>();
        serviceCollection.AddTransient<IValidator<SampleRequest>, HandlerLevelValidator>();

        var container = new MicrosoftBenzeneServiceContainer(serviceCollection);
        container.AddFluentValidation(new[] { typeof(HandlerLevelValidator) });

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        pipeline.UseMessageHandlers(x => x
            .UseFluentValidation()
            .AddMessageHandler<SampleHandler, SampleRequest, string>("test")
        );

        var app = new BenzeneMessageApplication(pipeline.Build());
        var request = new BenzeneMessageRequest
        {
            Topic = "test",
            Body = "{\"Name\":\"\"}"
        };

        var response = await app.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));

        Assert.Equal(BenzeneResultStatus.ValidationError, response.StatusCode);
    }

    private class CustomDefaultStatuses : IDefaultStatuses
    {
        public string ValidationError => "CustomValidationError";
        public string NotFound => BenzeneResultStatus.NotFound;
        public string BadRequest => BenzeneResultStatus.BadRequest;
        public string UnhandledException => BenzeneResultStatus.ServiceUnavailable;
    }

    [Fact]
    public async Task ValidationWithNoExplicitStatus_OverriddenIDefaultStatuses_AppliesAcrossThePipeline()
    {
        // ServiceResolverMother.CreateServiceCollection() already ran AddBenzene()'s
        // TryAddSingleton<IDefaultStatuses, DefaultStatuses>(); registering a second IDefaultStatuses
        // on top of it (the last registration wins) is exactly the top-level override the framework
        // supports - not a per-handler attribute.
        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.AddSingleton<IDefaultStatuses>(new CustomDefaultStatuses());
        serviceCollection.UsingBenzene(x => x.AddBenzeneMessage());
        serviceCollection.AddTransient<SampleHandler>();
        serviceCollection.AddTransient<IValidator<SampleRequest>, HandlerLevelValidator>();

        var container = new MicrosoftBenzeneServiceContainer(serviceCollection);
        container.AddFluentValidation(new[] { typeof(HandlerLevelValidator) });

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        pipeline.UseMessageHandlers(x => x
            .UseFluentValidation()
            .AddMessageHandler<SampleHandler, SampleRequest, string>("test")
        );

        var app = new BenzeneMessageApplication(pipeline.Build());
        var request = new BenzeneMessageRequest
        {
            Topic = "test",
            Body = "{\"Name\":\"\"}"
        };

        var response = await app.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceCollection.BuildServiceProvider()));

        Assert.Equal("CustomValidationError", response.StatusCode);
    }
}
