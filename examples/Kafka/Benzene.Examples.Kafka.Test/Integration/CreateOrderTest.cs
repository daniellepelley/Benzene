using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Kafka.Test.Helpers;
using Benzene.Examples.Kafka.Test.Helpers.Builders;
using Xunit;

namespace Benzene.Examples.Kafka.Test.Integration;

[Trait("Category", "Integration")]
[Collection("Sequential")]
public class CreateOrderTest : InMemoryOrdersTestBase
{
    private const string CreateOrder = MessageTopicNames.OrderCreate;

    private static CreateOrderMessage CreateCreateOrderMessage()
    {
        return new CreateOrderMessage { Status = Defaults.Order.Status, Name = Defaults.Order.Name };
    }

    [Fact]
    public async Task CreateOrder_Kafka_Message()
    {
        await KafkaSetUp.SendAsync(CreateOrder, CreateCreateOrderMessage());
        await ResultPoller.Poll(100, 100, () => GetPersistedOrders().Any(), "No results");
        
        var orders = GetPersistedOrders();
        Assert.Single(orders);
        Assert.Equal(Defaults.Order.Status, orders[0].Status);
        Assert.Equal(Defaults.Order.Name, orders[0].Name);
    }

    [Fact]
    public async Task CreateOrder_Kafka_Message_Multiple()
    {
        await KafkaSetUp.SendAsync(CreateOrder, CreateCreateOrderMessage());
        await KafkaSetUp.SendAsync(CreateOrder, CreateCreateOrderMessage());
        await KafkaSetUp.SendAsync(CreateOrder, CreateCreateOrderMessage());

        await ResultPoller.Poll(100, 100, () => GetPersistedOrders().Length == 3, "No results");
        
        var orders = GetPersistedOrders();
        Assert.Equal(3, orders.Length);
        Assert.Equal(Defaults.Order.Status, orders[0].Status);
        Assert.Equal(Defaults.Order.Name, orders[0].Name);
    }
}