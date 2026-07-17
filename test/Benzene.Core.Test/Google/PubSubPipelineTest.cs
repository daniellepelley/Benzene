using System.Threading.Tasks;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.GoogleCloud.Functions.PubSub;
using Benzene.GoogleCloud.Functions.PubSub.TestHelpers;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Google;

public interface IPubSubTestService
{
    void Register(string name);
}

public class PubSubTestRequest
{
    public string Name { get; set; }
}

// Deliberately not annotated with [Message]: see PingHandler's comment in
// Hosting/AspNetUnifiedStartUpTest.cs for why (avoids polluting AppDomain-wide reflection scans in
// unrelated tests).
public class PubSubTestHandler : IMessageHandler<PubSubTestRequest, Void>
{
    private readonly IPubSubTestService _service;

    public PubSubTestHandler(IPubSubTestService service)
    {
        _service = service;
    }

    public Task<IBenzeneResult<Void>> HandleAsync(PubSubTestRequest request)
    {
        _service.Register(request.Name);
        return Task.FromResult(BenzeneResult.Ok(new Void()));
    }
}

public class PubSubTestStartUp : BenzeneStartUp
{
    public const string Topic = "pubsub-test-topic";

    public override IConfiguration GetConfiguration() => new ConfigurationBuilder().Build();

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.UsingBenzene(x => x
            .AddBenzene()
            .AddMessageHandlers()
            .AddScoped<PubSubTestHandler>()
            .AddSingleton<IMessageHandlerDefinition>(_ => MessageHandlerDefinition.CreateInstance(
                Topic, "", typeof(PubSubTestRequest), typeof(Void), typeof(PubSubTestHandler))));

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration) => app
        .UsePubSub(pubsub => pubsub.UseMessageHandlers());
}

public class PubSubPipelineTest
{
    [Fact]
    public async Task SendPubSubAsync_RoutesThroughUseMessageHandlers()
    {
        var mockService = new Mock<IPubSubTestService>();

        var function = BenzeneTestHost.Create<PubSubTestStartUp>()
            .WithServices(services => services.AddSingleton(mockService.Object))
            .BuildGooglePubSubFunctionHost();

        var data = MessageBuilder.Create(PubSubTestStartUp.Topic, new PubSubTestRequest { Name = "world" }).AsPubSubEvent();

        await function.SendPubSubAsync(data);

        mockService.Verify(x => x.Register("world"));
    }
}
