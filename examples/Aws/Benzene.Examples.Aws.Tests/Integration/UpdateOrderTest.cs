using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.TestUtilities;
using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Aws.Tests.Helpers;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Benzene.Aws.Lambda.Sns;
using Benzene.Examples.Aws.Tests.Helpers.Builders;
using Benzene.Testing;
using Xunit;
using ThreadSafeTestLambdaLogger = Benzene.Examples.Aws.Tests.Helpers.ThreadSafeTestLambdaLogger;

namespace Benzene.Examples.Aws.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Sequential")]
public class UpdateOrderTest : InMemoryOrdersTestBase
{
    private const string CreateOrder = MessageTopicNames.OrderCreate;
    private const string UpdateOrder = MessageTopicNames.OrderUpdate;
    private const string UpdatedStatus = "updated-crn";
    private const string UpdatedName = "updated-name";

    public UpdateOrderTest()
        :base(BenzeneTestHost.Create<StartUp>().BuildAwsLambdaHost())
    { }
    
    private CreateOrderMessage CreateOrderMessage()
    {
        return new CreateOrderMessage
        {
            Name = Defaults.Order.Name,
            Status = Defaults.Order.Status,
        };
    }

    [Fact]
    public async Task UpdateOrder_SNSEvent()
    {
        //create order event
        var snsEvent = AwsEventBuilder.CreateSnsEvent(CreateOrder, CreateOrderMessage());
        await TestLambdaHosting.SendEventAsync(snsEvent);
        var orders = GetPersistedOrders().First();

        //Update order
        var updatedJsonStr = new { orders.Id, Status = UpdatedStatus, Name = UpdatedName };
        var snsUpdateEvent = AwsEventBuilder.CreateSnsEvent(UpdateOrder, updatedJsonStr);
        await TestLambdaHosting.SendEventAsync(snsUpdateEvent);
        var updatedOrders = GetPersistedOrders().ToArray();

        Assert.Single(updatedOrders);
        Assert.Equal(UpdatedStatus, updatedOrders[0].Status);
        Assert.Equal(UpdatedName, updatedOrders[0].Name);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal("204", messages[2].GetStatus());
        // Assert.Equal($"{UpdateOrder}:result", messages[2].GetTopic());
        //
        // Assert.Equal($"{UpdateOrder}d", messages[3].GetTopic());
        // Assert.Null(messages[3].GetStatus());
        // var payload = messages[3].Body<OrderDto>();
        // Assert.Equal(UpdatedStatus, payload.Status);
        // Assert.Equal(UpdatedName, payload.Name);
    }

    [Fact]
    public async Task UpdateOrder_SQSEvent()
    {
        //create person event
        var sqsCreateEvent = AwsEventBuilder.CreateSqsEvent(CreateOrder, CreateOrderMessage());
        await TestLambdaHosting.SendEventAsync(sqsCreateEvent);
        var orders = GetPersistedOrders().First();

        //Update order
        var updatedJsonStr = new { orders.Id, Status = UpdatedStatus, Name = UpdatedName };
        var snsUpdateEvent = AwsEventBuilder.CreateSqsEvent(UpdateOrder, updatedJsonStr);
        await TestLambdaHosting.SendEventAsync(snsUpdateEvent);
        var updatedOrders = GetPersistedOrders().ToArray();

        Assert.Single(updatedOrders);
        Assert.Equal(UpdatedStatus, updatedOrders[0].Status);
        Assert.Equal(UpdatedName, updatedOrders[0].Name);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal("204", messages[2].GetStatus());
        // Assert.Equal($"{UpdateOrder}:result", messages[2].GetTopic());
        //
        // Assert.Equal($"{UpdateOrder}d", messages[3].GetTopic());
        // Assert.Null(messages[3].GetStatus());
        // var payload = messages[3].Body<OrderDto>();
        // Assert.Equal(UpdatedStatus, payload.Status);
        // Assert.Equal(UpdatedName, payload.Name);
    }

    [Fact]
    public async Task UpdateOrder_ValidationFailure()
    {
        var snsUpdateEvent = AwsEventBuilder.CreateSnsEvent(UpdateOrder, new UpdateOrderMessage { Status = "" });

        // Settlement is safe-by-default: a handler that returns a failure result is escalated to a
        // thrown exception so SNS redelivers the message instead of silently acking it.
        await Assert.ThrowsAsync<SnsMessageProcessingException>(
            () => TestLambdaHosting.SendEventAsync(snsUpdateEvent));
    }

    [Fact]
    public async Task UpdateOrder_InvalidFKi_NotFound()
    {
        //Update an order that was never created
        var updatedJsonStr = new { Defaults.Order.Id, Status = UpdatedStatus, Name = UpdatedName };
        var snsUpdateEvent = AwsEventBuilder.CreateSnsEvent(UpdateOrder, updatedJsonStr);

        await Assert.ThrowsAsync<SnsMessageProcessingException>(
            () => TestLambdaHosting.SendEventAsync(snsUpdateEvent));

        Assert.Empty(GetPersistedOrders());
    }

    [Fact]
    public void UpdateOrder_ThreadSafety()
    {
        var snsEvent = AwsEventBuilder.CreateSnsEvent(CreateOrder, CreateOrderMessage());
        var sqsEvent = AwsEventBuilder.CreateSqsEvent(CreateOrder, CreateOrderMessage());

        var lambdaContext = new TestLambdaContext();
        var testLogger = new ThreadSafeTestLambdaLogger();
        lambdaContext.Logger = testLogger;
        var threads1 = Enumerable.Range(0, 10)
            .Select(_ => new Thread(() => TestLambdaHosting.SendEventAsync(snsEvent, lambdaContext).Wait())).ToArray();
        var threads2 = Enumerable.Range(0, 10)
            .Select(_ => new Thread(() => TestLambdaHosting.SendEventAsync(sqsEvent, lambdaContext).Wait())).ToArray();

        foreach (var thread in threads1.Concat(threads2))
        {
            thread.Start();
        }

        foreach (var thread in threads1.Concat(threads2))
        {
            thread.Join(1000);
        }

        var orders = GetPersistedOrders().ToArray();

        Assert.Equal(20, orders.Length);

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(Defaults.Order.Status, orders[i].Status);
            Assert.Equal(Defaults.Order.Name, orders[i].Name);
        }
    }
}
