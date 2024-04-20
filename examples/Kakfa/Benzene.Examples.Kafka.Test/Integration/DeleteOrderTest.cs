using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Kafka.Test.Helpers;
using Benzene.Examples.Kafka.Test.Helpers.Builders;
using Xunit;

namespace Benzene.Examples.Kafka.Test.Integration;

[Trait("Category", "Integration")]
[Collection("Sequential")]
public class DeleteOrderTest : InMemoryOrdersTestBase
{
    private const string CreateOrder = MessageTopicNames.OrderCreate;
    private const string DeleteOrder = MessageTopicNames.OrderDelete;

    private static DeleteOrderMessage CreateDeleteOrderMessage(string orderId)
    {
        return new DeleteOrderMessage()
        {
            Id = orderId
        };
    }

    private CreateOrderMessage CreateCreateOrderMessage()
    {
        return new CreateOrderMessage
        {
            Name = Defaults.Order.Name,
            Status = Defaults.Order.Status,
        };
    }

    [Fact]
    public async Task DeleteOrder_Kafka_Message()
    {
        //Create Order event
        await KafkaSetUp.SendAsync(CreateOrder, CreateCreateOrderMessage());
        await ResultPoller.Poll(100, 100, () => GetPersistedOrders().Any(), "No results");
        var order = GetPersistedOrders().First();

        //Delete Order event
        await KafkaSetUp.SendAsync(DeleteOrder, CreateDeleteOrderMessage(order.Id.ToString()));
        await ResultPoller.Poll(100, 100, () => !GetPersistedOrders().Any(), "No results");
        var orders = GetPersistedOrders().ToArray();

        Assert.Empty(orders);
    }
}