using System.Net;
using Benzene.Example.Asp.Test.Helpers;
using Benzene.Example.Asp.Test.Helpers.Builders;
using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model.Messages;
using Xunit;

namespace Benzene.Example.Asp.Test.Integration;

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
    public async Task CreateOrder_ApiGateway()
    {
        await _client.SendAsync(new RequestBuilder(HttpMethod.Post, "/orders")
            .WithBody(CreateCreateOrderMessage())
            .Build());

        // await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(createRequest);

        var order = GetPersistedOrders().First();
        Assert.NotNull(order);

        var response = await _client.SendAsync(new RequestBuilder(HttpMethod.Delete, $"/orders/{order.Id}")
            .WithBody(CreateCreateOrderMessage())
            .Build());

        // var response = await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);
        //Update Order event
           
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var orders = GetPersistedOrders();

        Assert.Empty(orders);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{DeleteOrder}d", messages[1].GetTopic());
        // Assert.Null(messages[1].GetStatus());
        // Assert.True(messages[1].BodyIsGuid());
    }

    // [Fact]
    // public async Task DeleteOrder_SQSEvent_WhenDoesNotExist()
    // {
    //     var orderId = Guid.NewGuid();
    //
    //     var sqsDeleteEvent = AwsEventBuilder.CreateSqsEvent(DeleteOrder, CreateDeleteOrderMessage(orderId.ToString()));
    //     await TestLambdaHosting.SendEventAsync(sqsDeleteEvent);
    //
    //     var messages = await SqsSetUp.GetAllMessagesAsync();
    //     Assert.Equal($"{DeleteOrder}:result", messages[0].GetTopic());
    //
    //     var errorPayload = messages[0].Body<ErrorPayload>();
    //     Assert.Equal(Defaults.ErrorStatus.NotFound, errorPayload.Status);
    //
    //     Assert.Single(messages);
    // }

    // [Fact]
    // public async Task DeleteOrder_SQSEvent_WhenAlreadyDeleted()
    // {
    //     var orderId = Guid.NewGuid();
    //
    //     var sqsDeleteEvent = AwsEventBuilder.CreateSqsEvent(DeleteOrder, CreateDeleteOrderMessage(orderId.ToString()));
    //     await TestLambdaHosting.SendEventAsync(sqsDeleteEvent);
    //
    //     var messages = await SqsSetUp.GetAllMessagesAsync();
    //     Assert.Equal($"{DeleteOrder}:result", messages[0].GetTopic());
    //     
    //     var errorPayload = messages[0].Body<ErrorPayload>();
    //     Assert.Equal(Defaults.ErrorStatus.NotFound, errorPayload.Status);
    // }
    //
    // [Fact]
    // public async Task DeleteOrder_ValidationFailure()
    // {
    //     //Update Order event
    //     var snsDeleteEvent = AwsEventBuilder.CreateSqsEvent(DeleteOrder, new CreateOrderMessage());
    //     await TestLambdaHosting.SendEventAsync(snsDeleteEvent);
    //     
    //     var messages = await SqsSetUp.GetAllMessagesAsync();
    //     Assert.Equal($"{DeleteOrder}:result", messages[0].GetTopic());
    //     
    //     var errorPayload = messages[0].Body<ErrorPayload>();
    //     Assert.Equal(Defaults.ErrorStatus.ValidationError, errorPayload.Status);
    //     Assert.NotEmpty(errorPayload.Errors);
    // }
}