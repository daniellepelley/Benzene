using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Validation;
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

    [ValidationStatus(BenzeneResultStatus.Forbidden)]
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
    public async Task ValidationWithStatusFromHandlerAttribute()
    {
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

        Assert.Equal(BenzeneResultStatus.Forbidden, response.StatusCode);
    }
}
