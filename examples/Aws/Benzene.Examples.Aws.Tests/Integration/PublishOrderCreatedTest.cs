using System;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Benzene.Clients;
using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Aws.Tests.Helpers;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Examples.Aws.Tests.Integration;

/// <summary>
/// Proves the ingress/egress symmetry demo (release plan Tier 2.3) is actually reachable, in
/// isolation from the order-persistence tests in <c>CreateOrderTest</c>. Regression coverage for a
/// real bug: <c>DependenciesBuilder</c>'s <c>.AddMessageHandlers(...)</c> only scanned the shared
/// App domain's assembly, so <c>PublishOrderCreatedMessageHandler</c> (defined in this project's
/// own assembly) was never discoverable - no topic route, no HTTP route - despite compiling and
/// looking wired. No existing test in this suite called it before this one.
/// </summary>
[Trait("Category", "Integration")]
[Collection("Sequential")]
public class PublishOrderCreatedTest
{
    [Fact]
    public async Task PublishOrderCreated_ApiGateway_SendsOnTheOrderCreatedTopic()
    {
        EnvironmentSetUp.SetUp();
        var fakeSender = new FakeBenzeneMessageSender();
        var entryPoint = BenzeneTestHost.Create<StartUp>()
            .WithServices(services => services.AddSingleton<IBenzeneMessageSender>(fakeSender))
            .BuildAwsLambdaHost();
        using var testLambdaHosting = new AwsLambdaBenzeneTestHost(entryPoint);

        var orderCreated = new OrderCreatedEvent { Id = Guid.NewGuid(), Name = "acme" };
        var apiGatewayProxyRequest = new ApiGatewayProxyRequestBuilder("POST", "/orders/publish-created")
            .WithBody(orderCreated)
            .Build();

        var response = await testLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);

        Assert.Equal(202, response.StatusCode);
        Assert.Equal(MessageTopicNames.OrderCreated, fakeSender.LastTopic);

        // Assert the message *body* too, not just the topic - the egress demo's whole point is that
        // the OrderCreatedEvent handed to the handler is what gets published on the wire. This mirrors
        // the Azure example's egress test (Egress_PublishOrderCreated_SendsOnTheOrderCreatedTopic), so
        // both hosts prove ingress->handler->egress carries the payload through, not only the topic.
        var sent = Assert.IsType<OrderCreatedEvent>(fakeSender.LastRequest);
        Assert.Equal(orderCreated.Id, sent.Id);
        Assert.Equal("acme", sent.Name);
    }
}
