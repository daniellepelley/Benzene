using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Benzene.Core.Results;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Aws.Tests.Helpers;
using Benzene.Examples.Aws.Tests.Helpers.Builders;
using Benzene.Xml;
using Xunit;

namespace Benzene.Examples.Aws.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Sequential")]
public class GetOrderTest : InMemoryOrdersTestBase
{
    private const string GetOrder = MessageTopicNames.OrderGet;
    private readonly Guid _id = Guid.Parse(Defaults.Order.Id);

    public GetOrderTest()
        :base(new TestLambdaStartUp<StartUp>().Build())
    { }
    
    private GetOrderMessage GetOrderMessage()
    {
        return new GetOrderMessage { Id = _id.ToString()};
    }

    [Fact]
    public async Task GetOrder_SNSEvent()
    {
        AddOrder(new Order
        {
            Id = _id,
            Status = Defaults.Order.Status,
        });

        var snsEvent = AwsEventBuilder.CreateSnsEvent(GetOrder, GetOrderMessage());

        await TestLambdaHosting.SendEventAsync(snsEvent);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{GetOrder}:result", messages[0].GetTopic());
        // Assert.Equal("200", messages[0].GetStatus());
    }

    [Fact]
    public async Task GetOrder_SQSEvent()
    {
        SetUpDatabase();

        var sqsEvent = AwsEventBuilder.CreateSqsEvent(GetOrder, GetOrderMessage());

        await TestLambdaHosting.SendEventAsync(sqsEvent);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{GetOrder}:result", messages[0].GetTopic());
        // Assert.Equal("200", messages[0].GetStatus());
    }


    [Fact]
    public async Task GetOrder_ApiGateway()
    {
        SetUpDatabase();
        var databaseOrder= GetPersistedOrders().First();
            
        var apiGatewayProxyRequest = new ApiGatewayProxyRequestBuilder("GET", $"/orders/{databaseOrder.Id}")
            .Build();

        var response = await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);
            
        var order = response.Body<Order>();

        Assert.Equal(databaseOrder.Id, order.Id);
    }

    [Fact]
    public async Task GetOrder_ApiGateway_Xml()
    {
        SetUpDatabase();
        var databaseOrder = GetPersistedOrders().First();

        var apiGatewayProxyRequest = new ApiGatewayProxyRequestBuilder("GET", $"/orders/{databaseOrder.Id}")
            .WithHeader("content-type", "application/xml")
            .Build();

        var response = await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);

        var order = new XmlSerializer().Deserialize<OrderDto>(response.Body);

        Assert.Equal(databaseOrder.Id, order.Id);

        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
    }

    [Fact]
    public async Task GetOrder_ApiGateway_ValidationError_Xml()
    {
        SetUpDatabase();

        var apiGatewayProxyRequest = new ApiGatewayProxyRequestBuilder("GET", $"/orders/foo")
            .WithHeader("content-type", "application/xml")
            .Build();

        var response = await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);

        var order = new XmlSerializer().Deserialize<ErrorPayload>(response.Body);

        Assert.Equal("ValidationError", order.Status);

        Assert.Equal(422, response.StatusCode);
        Assert.NotNull(response.Body);
    }

    // [Fact]
    // public async Task GetOrder_ApiGateway_BenzeneMessage()
    // {
    //     SetUpDatabase();
    //     var databaseOrder= GetPersistedOrders().First();
    //         
    //     var apiGatewayProxyRequest = new ApiGatewayProxyRequestBuilder("POST", $"/admin/benzene-message")
    //         .WithBody(new
    //         {
    //             topic = GetOrder,
    //             body = JsonConvert.SerializeObject(new { id = databaseOrder.Id }),
    //         })
    //         .Build();
    //
    //     var response = await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);
    //         
    //     var order = response.Body<Order>();
    //
    //     Assert.Equal(databaseOrder.Id, order.Id);
    // }


    [Fact]
    public async Task GetOrder_SQSEvent_WhenDeleted()
    {
        var sqsEvent = AwsEventBuilder.CreateSqsEvent(GetOrder, GetOrderMessage());

        await TestLambdaHosting.SendEventAsync(sqsEvent);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{GetOrder}:result", messages[0].GetTopic());
        // var errorPayload = messages[0].Body<ErrorPayload>();
        // Assert.Equal(Defaults.ErrorStatus.NotFound, errorPayload.Status);
    }

    [Fact]
    public async Task GetOrder_ValidationFailure()
    {
        var apiGatewayProxyRequest = new ApiGatewayProxyRequestBuilder("GET", $"/orders/foo")
            .Build();

        var response = await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);
           
        Assert.Equal(422, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_ThreadSafety()
    {
        AddOrder(new Order
        {
            Id = _id,
            Status = Defaults.Order.Status,
        });

        var snsEvent = AwsEventBuilder.CreateSnsEvent(GetOrder, GetOrderMessage());
        var sqsEvent = AwsEventBuilder.CreateSqsEvent(GetOrder, GetOrderMessage());

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
            thread.Join();
        }

        await Task.Delay(2000);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{GetOrder}:result", messages[0].GetTopic());
        // Assert.Equal(20, messages.Length);
    }

    private void SetUpDatabase()
    {
        AddOrder(new Order
        {
            Id = _id,
            Status = Defaults.Order.Status,
        });
    }
}