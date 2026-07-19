using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Extras.ResponseEvents;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Extras.ResponseEvents;

// Note: the handler deliberately reuses ExampleRequestPayload for both request and response (like
// DynamoDbMessagePipelineTest's handler does), so whole-assembly spec-generation tests
// (SpecTest) see no new payload schemas. The payload's Name carries the desired result kind.
[Message("order:create")]
public class ResponseEventsCreateOrderHandler : IMessageHandler<ExampleRequestPayload, ExampleRequestPayload>
{
    public Task<IBenzeneResult<ExampleRequestPayload>> HandleAsync(ExampleRequestPayload request)
    {
        return Task.FromResult(request.Name switch
        {
            "ok" => BenzeneResult.Ok(new ExampleRequestPayload { Id = request.Id, Name = request.Name }),
            "nopayload" => BenzeneResult.Accepted<ExampleRequestPayload>(),
            "notfound" => BenzeneResult.NotFound<ExampleRequestPayload>(),
            _ => BenzeneResult.Created(new ExampleRequestPayload { Id = request.Id, Name = request.Name }),
        });
    }
}

public class ResponseEventsPipelineTest
{
    private static async Task<(IBenzeneMessageResponse Response, List<OutboundContext> Published, IServiceResolver Resolver)> RunAsync(
        string mode,
        System.Action<ResponseEventsBuilder> configureEvents,
        bool routePublishedTopic = true)
    {
        var published = new List<OutboundContext>();

        var serviceCollection = ServiceResolverMother.CreateServiceCollection();
        serviceCollection.UsingBenzene(x =>
        {
            x.AddBenzeneMessage();
            x.AddOutboundRouting(routing => routing
                .Route(routePublishedTopic ? "order:created" : "some:other-topic", pipeline => pipeline
                    .OnRequest(context =>
                    {
                        published.Add(context);
                        context.Response = BenzeneResult.Accepted<Void>();
                    })));
        });

        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(serviceCollection));
        pipeline.UseMessageHandlers(new[] { typeof(ResponseEventsCreateOrderHandler) }, router => router
            .UseResponseEvents(configureEvents));

        var application = new BenzeneMessageApplication(pipeline.Build());

        var request = new BenzeneMessageRequest
        {
            Topic = "order:create",
            Body = JsonConvert.SerializeObject(new ExampleRequestPayload { Id = 42, Name = mode })
        };

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var response = await application.HandleAsync(request, new MicrosoftServiceResolverFactory(serviceProvider));

        return (response, published, new MicrosoftServiceResolverAdapter(serviceProvider));
    }

    [Fact]
    public async Task Map_SuccessfulResponse_PublishesEventAndKeepsResponse()
    {
        var (response, published, _) = await RunAsync("created", events => events
            .Map("order:create", "order:created"));

        Assert.Equal(BenzeneResultStatus.Created, response.StatusCode);
        var context = Assert.Single(published);
        Assert.Equal("order:created", context.Topic);
        Assert.Equal(42, ((ExampleRequestPayload)context.Request).Id);
    }

    [Fact]
    public async Task Map_FailedResponse_DoesNotPublish()
    {
        var (response, published, _) = await RunAsync("notfound", events => events
            .Map("order:create", "order:created"));

        Assert.Equal(BenzeneResultStatus.NotFound, response.StatusCode);
        Assert.Empty(published);
    }

    [Fact]
    public async Task Map_SuccessfulResponseWithoutPayload_DoesNotPublish()
    {
        var (response, published, _) = await RunAsync("nopayload", events => events
            .Map("order:create", "order:created"));

        Assert.Equal(BenzeneResultStatus.Accepted, response.StatusCode);
        Assert.Empty(published);
    }

    [Fact]
    public async Task Map_WhenPredicate_OnlyPublishesWhenPredicateMatches()
    {
        System.Action<ResponseEventsBuilder> configure = events => events
            .Map("order:create", "order:created", when: result => result.Status == BenzeneResultStatus.Created);

        var (_, publishedOnOk, _) = await RunAsync("ok", configure);
        var (_, publishedOnCreated, _) = await RunAsync("created", configure);

        Assert.Empty(publishedOnOk);
        Assert.Single(publishedOnCreated);
    }

    [Fact]
    public async Task MapCrudConvention_CreatedResult_PublishesPastTenseTopic()
    {
        var (_, published, _) = await RunAsync("created", events => events.MapCrudConvention());

        var context = Assert.Single(published);
        Assert.Equal("order:created", context.Topic);
    }

    [Fact]
    public async Task MapCrudConvention_OkResult_DoesNotPublish()
    {
        var (_, published, _) = await RunAsync("ok", events => events.MapCrudConvention());

        Assert.Empty(published);
    }

    [Fact]
    public async Task UnroutedEventTopic_FailMessage_ReplacesResponseWithUnexpectedError()
    {
        var (response, published, _) = await RunAsync("created", events => events
            .Map("order:create", "order:created"), routePublishedTopic: false);

        Assert.Equal(BenzeneResultStatus.UnexpectedError, response.StatusCode);
        Assert.Empty(published);
    }

    [Fact]
    public async Task UnroutedEventTopic_LogAndContinue_KeepsHandlerResponse()
    {
        var (response, published, _) = await RunAsync("created", events => events
            .Map("order:create", "order:created")
            .OnPublishFailure(PublishFailureMode.LogAndContinue), routePublishedTopic: false);

        Assert.Equal(BenzeneResultStatus.Created, response.StatusCode);
        Assert.Empty(published);
    }

    [Fact]
    public async Task Catalog_IsResolvableAndListsRegisteredMappings()
    {
        var (_, _, resolver) = await RunAsync("created", events => events
            .Map<ExampleRequestPayload>("order:create", "order:created"));

        var catalog = resolver.GetService<IResponseEventCatalog>();

        var mapping = Assert.Single(catalog.Mappings);
        Assert.Equal("order:create", mapping.SourceTopic);
        Assert.Equal("order:created", mapping.EventTopic);
        Assert.Equal(typeof(ExampleRequestPayload), mapping.PayloadType);

        var definitions = Assert.IsAssignableFrom<IMessageDefinitionFinder<IMessageDefinition>>(catalog).FindDefinitions();
        var definition = Assert.Single(definitions);
        Assert.Equal("order:created", definition.Topic.Id);
        Assert.Equal(typeof(ExampleRequestPayload), definition.RequestType);
    }
}
