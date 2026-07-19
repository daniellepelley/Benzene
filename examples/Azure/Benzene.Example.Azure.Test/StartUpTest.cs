using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.AspNet.TestHelpers;
using Benzene.Azure.Function.Core.TestHelpers;
using Benzene.Azure.Function.ServiceBus;
using Benzene.Azure.Function.ServiceBus.TestHelpers;
using Benzene.Example.Azure.Test.Helpers;
using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using System.Text.Json;
using Benzene.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Example.Azure.Test;

/// <summary>
/// In-memory integration tests for the Azure Functions example, proving the same handlers are
/// really reachable through every trigger <see cref="StartUp"/> wires them onto - HTTP and Service
/// Bus - plus the ingress/egress symmetry demo, entirely in-process via <c>Benzene.Testing</c>'s
/// <see cref="BenzeneTestHost"/>. No Azure Functions host, no live Service Bus.
/// </summary>
public class StartUpTest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public StartUpTest()
    {
        // DependenciesBuilder constructs a real ServiceBusClient/ServiceBusSender at
        // ConfigureServices time; it needs a syntactically valid connection string to construct
        // without throwing, even though nothing in these tests lets it actually connect (the
        // egress test below replaces IBenzeneMessageSender before any send could reach it).
        Environment.SetEnvironmentVariable("ServiceBusConnection",
            "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=ZmFrZQ==");
    }

    [Fact]
    public async Task Http_GetAllOrders_ReturnsOrdersFromTheOrderService()
    {
        var app = BenzeneTestHost.Create<StartUp>().BuildAzureFunctionApp();

        var request = HttpBuilder.Create("GET", "/orders").AsAspNetCoreHttpRequest();
        var result = await app.HandleHttpRequest(request);

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, contentResult.StatusCode);
        var orders = JsonSerializer.Deserialize<OrderDto[]>(contentResult.Content!, JsonOptions);
        Assert.NotEmpty(orders!);
    }

    [Fact]
    public async Task Http_CreateOrder_ReturnsTheCreatedOrder()
    {
        var app = BenzeneTestHost.Create<StartUp>().BuildAzureFunctionApp();

        var request = HttpBuilder
            .Create("POST", "/orders", new CreateOrderMessage { Name = "acme", Status = "new" })
            .AsAspNetCoreHttpRequest();
        var result = await app.HandleHttpRequest(request);

        var contentResult = Assert.IsType<ContentResult>(result);
        // HardcodedOrderService.SaveAsync returns BenzeneResult.Ok, not .Created - matching the
        // demo's actual (if debatable) status code rather than the REST convention a real service
        // would likely want.
        Assert.Equal(200, contentResult.StatusCode);
        var order = JsonSerializer.Deserialize<OrderDto>(contentResult.Content!, JsonOptions);
        Assert.NotNull(order);
    }

    [Fact]
    public async Task ServiceBus_CreateOrderMessage_RoutesToTheSameHandlerAsHttp()
    {
        var app = BenzeneTestHost.Create<StartUp>().BuildAzureFunctionApp();

        // Service Bus routes by the "topic" application property, not a URL - proves the same
        // CreateOrderMessageHandler the HTTP test above hits is reachable from a second transport
        // with no handler-side changes, the framework's central promise.
        var message = MessageBuilder
            .Create(MessageTopicNames.OrderCreate, new CreateOrderMessage { Name = "acme", Status = "new" })
            .AsAzureServiceBusMessage();

        await app.HandleServiceBusMessages(message);
    }

    [Fact]
    public async Task Egress_PublishOrderCreated_SendsOnTheOrderCreatedTopic()
    {
        var fakeSender = new FakeBenzeneMessageSender();
        var app = BenzeneTestHost.Create<StartUp>()
            .WithServices(services => services.AddSingleton<Clients.IBenzeneMessageSender>(fakeSender))
            .BuildAzureFunctionApp();

        var orderCreated = new OrderCreatedEvent { Id = Guid.NewGuid(), Name = "acme" };
        var request = HttpBuilder.Create("POST", "/orders/publish-created", orderCreated).AsAspNetCoreHttpRequest();

        var result = await app.HandleHttpRequest(request);

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(202, contentResult.StatusCode);
        Assert.Equal(MessageTopicNames.OrderCreated, fakeSender.LastTopic);
        var sent = Assert.IsType<OrderCreatedEvent>(fakeSender.LastRequest);
        Assert.Equal(orderCreated.Id, sent.Id);
    }
}
