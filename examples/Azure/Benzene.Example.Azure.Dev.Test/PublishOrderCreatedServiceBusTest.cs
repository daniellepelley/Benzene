using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.AspNet.TestHelpers;
using Benzene.Azure.Function.Core.TestHelpers;
using Benzene.Example.Azure.Dev.Test.Fixtures;
using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model.Messages;
using Benzene.Testing;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Benzene.Example.Azure.Dev.Test;

/// <summary>
/// The real-dependency counterpart of the in-memory <c>StartUpTest.Egress_PublishOrderCreated_*</c>:
/// it runs the Azure example's Service Bus <b>egress</b> demo (<c>POST /orders/publish-created</c> →
/// <c>PublishOrderCreatedMessageHandler</c> → the real <c>.UseServiceBus(sender)</c> outbound route)
/// against a real Azure Service Bus emulator, then drains the <c>orders</c> queue with the SDK to
/// prove a message actually landed on the wire with the right topic and payload - not a fake sender.
///
/// This tier needs Docker (the Service Bus emulator + its MSSQL backend), so this project is
/// deliberately kept out of Benzene.Examples.sln and run by its own CI job, following the .Dev.Test
/// convention - the Service Bus counterpart of the AWS LocalStack SQS tier.
/// </summary>
[Collection("Sequential")]
public class PublishOrderCreatedServiceBusTest : IClassFixture<ServiceBusEmulatorFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PublishOrderCreated_ActuallySendsToTheRealServiceBusQueue()
    {
        // 1. Point the example at the emulator BEFORE building the host - DependenciesBuilder builds a
        //    real ServiceBusClient/ServiceBusSender from configuration["ServiceBusConnection"] at
        //    ConfigureServices time, so the connection string has to be in place first.
        Environment.SetEnvironmentVariable("ServiceBusConnection", ServiceBusEmulatorFixture.ConnectionString);

        await using var client = new ServiceBusClient(ServiceBusEmulatorFixture.ConnectionString);

        // 2. Start from an empty queue (the fixture's readiness probe already drained itself, but a
        //    prior run in the same container could have left something behind).
        await DrainAsync(client);

        var app = BenzeneTestHost.Create<StartUp>().BuildAzureFunctionApp();

        // 3. Drive the egress handler over the same HTTP surface the in-memory test uses, exactly as deployed.
        var orderCreated = new OrderCreatedEvent { Id = Guid.NewGuid(), Name = "acme" };
        var request = HttpBuilder
            .Create("POST", "/orders/publish-created", orderCreated)
            .AsAspNetCoreHttpRequest();

        var result = await app.HandleHttpRequest(request);

        // The outbound Service Bus route maps its send-acknowledgement to BenzeneResultStatus.Accepted
        // (202) - see OutboundServiceBusContextConverter - matching the in-memory egress test.
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(202, contentResult.StatusCode);

        // 4. Drain the real queue and assert the message actually arrived on the wire, with the topic
        //    application property and JSON body the egress route puts there.
        var messages = await ReceiveAllAsync(client);
        var message = Assert.Single(messages);
        Assert.Equal(MessageTopicNames.OrderCreated, message.ApplicationProperties["topic"]);
        var delivered = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Body.ToString(), JsonOptions);
        Assert.Equal(orderCreated.Id, delivered!.Id);
        Assert.Equal("acme", delivered.Name);
    }

    private static async Task DrainAsync(ServiceBusClient client)
    {
        var receiver = client.CreateReceiver(ServiceBusEmulatorFixture.QueueName);
        while (true)
        {
            var batch = await receiver.ReceiveMessagesAsync(maxMessages: 10, maxWaitTime: TimeSpan.FromSeconds(2));
            if (batch.Count == 0)
            {
                return;
            }

            foreach (var message in batch)
            {
                await receiver.CompleteMessageAsync(message);
            }
        }
    }

    private static async Task<List<ServiceBusReceivedMessage>> ReceiveAllAsync(ServiceBusClient client)
    {
        var receiver = client.CreateReceiver(ServiceBusEmulatorFixture.QueueName);
        var all = new List<ServiceBusReceivedMessage>();

        // The send completes before the handler returns, but the emulator's receive side is still
        // eventually-consistent - poll briefly until the message shows up.
        for (var attempt = 0; attempt < 5 && all.Count == 0; attempt++)
        {
            var batch = await receiver.ReceiveMessagesAsync(maxMessages: 10, maxWaitTime: TimeSpan.FromSeconds(2));
            all.AddRange(batch);
        }

        foreach (var message in all)
        {
            await receiver.CompleteMessageAsync(message);
        }

        return all;
    }
}
