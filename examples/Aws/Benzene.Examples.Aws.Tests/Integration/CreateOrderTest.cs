using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Aws.Tests.Helpers;
using Benzene.Examples.Aws.Tests.Helpers.Builders;
using Benzene.Results;
using Benzene.Tools.Aws;
using Benzene.Xml;
using Newtonsoft.Json;
using Xunit;
using ThreadSafeTestLambdaLogger = Benzene.Examples.Aws.Tests.Helpers.ThreadSafeTestLambdaLogger;

namespace Benzene.Examples.Aws.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Sequential")]
public class CreateOrderTest : InMemoryOrdersTestBase
{
    private const string CreateOrder = MessageTopicNames.OrderCreate;

    public CreateOrderTest()
        :base(new AwsLambdaBenzeneTestStartUp<StartUp>().Build())
    { }
    
    private static CreateOrderMessage CreateCreateOrderMessage()
    {
        return new CreateOrderMessage { Status = Defaults.Order.Status, Name = Defaults.Order.Name };
    }

    [Fact]
    public async Task CreateOrder_SNSEvent()
    {
        var snsEvent = AwsEventBuilder.CreateSnsEvent(CreateOrder, CreateCreateOrderMessage());

        await TestLambdaHosting.SendEventAsync(snsEvent);
            
        var orders = GetPersistedOrders();

        Assert.Single(orders);
        Assert.Equal(Defaults.Order.Status, orders[0].Status);
        Assert.Equal(Defaults.Order.Name, orders[0].Name);
            
        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{CreateOrder}:result", messages[0].GetTopic());
        // Assert.True(messages[0].BodyIsGuid());
        //     
        // Assert.Equal($"{CreateOrder}d", messages[1].GetTopic());
        // Assert.Null(messages[1].GetStatus());
        // var payload = messages[1].Body<OrderDto>();
        // Assert.Equal(SomeStatus, payload.Status);
        // Assert.Equal(SomeName, payload.Name);
    }

    [Fact]
    public async Task CreateOrder_SNSEvent_Xml()
    {
        var snsEvent = new AwsEventBuilder()
            .WithTopic(CreateOrder)
            .WithXmlBody(CreateCreateOrderMessage())
            .CreateSnsEvent();

        await TestLambdaHosting.SendEventAsync(snsEvent);
            
        var orders = GetPersistedOrders();

        Assert.Single(orders);
        Assert.Equal(Defaults.Order.Status, orders[0].Status);
        Assert.Equal(Defaults.Order.Name, orders[0].Name);
            
        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{CreateOrder}:result", messages[0].GetTopic());
        // Assert.True(messages[0].BodyIsGuid());
        //     
        // Assert.Equal($"{CreateOrder}d", messages[1].GetTopic());
        // Assert.Null(messages[1].GetStatus());
        // var payload = messages[1].Body<OrderDto>();
        // Assert.Equal(SomeStatus, payload.Status);
        // Assert.Equal(SomeName, payload.Name);
    }

    [Fact]
    public async Task CreateOrder_SQSEvent()
    {
        var sqsEvent = AwsEventBuilder.CreateSqsEvent(CreateOrder, CreateCreateOrderMessage());

        await TestLambdaHosting.SendEventAsync(sqsEvent);

        var orders = GetPersistedOrders();

        Assert.Single(orders);
        Assert.Equal(Defaults.Order.Status, orders[0].Status);
        Assert.Equal(Defaults.Order.Name, orders[0].Name);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{CreateOrder}:result", messages[0].GetTopic());
        // Assert.True(messages[0].BodyIsGuid());
        //
        // Assert.Equal($"{CreateOrder}d", messages[1].GetTopic());
        // Assert.Null(messages[1].GetStatus());
        // var payload = messages[1].Body<OrderDto>();
        // Assert.Equal(SomeStatus, payload.Status);
        // Assert.Equal(SomeName, payload.Name);
    }

    [Fact]
    public async Task CreateOrder_SQSEvent_Xml()
    {
        var sqsEvent = new AwsEventBuilder()
            .WithTopic(CreateOrder)
            .WithXmlBody(CreateCreateOrderMessage())
            .CreateSqsEvent();

        await TestLambdaHosting.SendEventAsync(sqsEvent);

        var orders = GetPersistedOrders();

        Assert.Single(orders);
        Assert.Equal(Defaults.Order.Status, orders[0].Status);
        Assert.Equal(Defaults.Order.Name, orders[0].Name);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{CreateOrder}:result", messages[0].GetTopic());
        // Assert.True(messages[0].BodyIsGuid());
        //
        // Assert.Equal($"{CreateOrder}d", messages[1].GetTopic());
        // Assert.Null(messages[1].GetStatus());
        // var payload = messages[1].Body<OrderDto>();
        // Assert.Equal(SomeStatus, payload.Status);
        // Assert.Equal(SomeName, payload.Name);
    }

    [Fact]
    public async Task CreateOrder_BenzeneMessage()
    {
        var benzeneMessageRequest = BenzeneMessageBuilder.Create(CreateOrder, CreateCreateOrderMessage());

        var response = await TestLambdaHosting.SendEventAsync<BenzeneMessageResponse>(benzeneMessageRequest);
            
        var orders = GetPersistedOrders();

        Assert.Single(orders);
        Assert.Equal(Defaults.Order.Status, orders[0].Status);
        Assert.Equal(Defaults.Order.Name, orders[0].Name);

        Assert.Equal(BenzeneResultStatus.Created, response.StatusCode);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{CreateOrder}d", messages[0].GetTopic());
        // Assert.Null(messages[0].GetStatus());
        // var payload = messages[0].Body<OrderDto>();
        // Assert.Equal(SomeStatus, payload.Status);
        // Assert.Equal(SomeName, payload.Name);
    }

    [Fact]
    public async Task CreateOrder_ApiGateway()
    {
        var apiGatewayProxyRequest = new ApiGatewayProxyRequestBuilder("POST", "/orders")
            .WithBody(CreateCreateOrderMessage())
            .Build();
            
        var response = await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);
            
        var orders = GetPersistedOrders();

        Assert.Single(orders);
        Assert.Equal(Defaults.Order.Status, orders[0].Status);
        Assert.Equal(Defaults.Order.Name, orders[0].Name);

        Assert.Equal(201, response.StatusCode);
        Assert.NotNull(response.Body);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{CreateOrder}d", messages[0].GetTopic());
        // Assert.Null(messages[0].GetStatus());
        // var payload = messages[0].Body<OrderDto>();
        // Assert.Equal(SomeStatus, payload.Status);
        // Assert.Equal(SomeName, payload.Name);
    }

    [Fact]
    public async Task CreateOrder_ApiGateway_BenzeneMessage()
    {
        var apiGatewayProxyRequest = new ApiGatewayProxyRequestBuilder("POST", "admin/benzene-message")
            .WithBody(new BenzeneMessageRequest
            {
                Topic = CreateOrder,
                Body = JsonConvert.SerializeObject(CreateCreateOrderMessage())
            })
            .Build();
            
        var response = await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);
            
        var orders = GetPersistedOrders();

        Assert.Single(orders);
        Assert.Equal(Defaults.Order.Status, orders[0].Status);
        Assert.Equal(Defaults.Order.Name, orders[0].Name);

        Assert.Equal(201, response.StatusCode);
        Assert.NotNull(response.Body);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{CreateOrder}d", messages[0].GetTopic());
        // Assert.Null(messages[0].GetStatus());
        // var payload = messages[0].Body<OrderDto>();
        // Assert.Equal(SomeStatus, payload.Status);
        // Assert.Equal(SomeName, payload.Name);
    }

    [Fact]
    public async Task CreateOrder_ApiGateway_Xml()
    {
        var apiGatewayProxyRequest = new ApiGatewayProxyRequestBuilder("POST", "/orders")
            .WithXmlBody(CreateCreateOrderMessage())
            .WithHeader("content-type", "application/xml")
            .Build();
            
        var response = await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);

        var orders = GetPersistedOrders();

        Assert.Single(orders);
        Assert.Equal(Defaults.Order.Status, orders[0].Status);
        Assert.Equal(Defaults.Order.Name, orders[0].Name);

        Assert.Equal(201, response.StatusCode);
        Assert.NotNull(response.Body);

        var result = new XmlSerializer().Deserialize<OrderDto>(response.Body);
       
        Assert.Equal(Defaults.Order.Name, result.Name);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{CreateOrder}d", messages[0].GetTopic());
        // Assert.Null(messages[0].GetStatus());
        // var payload = messages[0].Body<OrderDto>();
        // Assert.Equal(SomeStatus, payload.Status);
        // Assert.Equal(SomeName, payload.Name);
    }


    [Fact]
    public async Task CreateOrder_ValidationFailure()
    {
        var benzeneMessageRequest = BenzeneMessageBuilder.Create(CreateOrder, new CreateOrderMessage { Status = "1234567890123456789012345678901234567890123456789012345678901234567890" });

        var response = await TestLambdaHosting.SendEventAsync<BenzeneMessageResponse>(benzeneMessageRequest);

        Assert.Equal(BenzeneResultStatus.ValidationError, response.StatusCode);
        Assert.NotNull(response.Body);

        var errorPayload = response.GetMessage<ErrorPayload>();
        Assert.Equal(Defaults.ErrorStatus.ValidationError, errorPayload.Status);
        Assert.NotEmpty(errorPayload.Detail);
    }

    [Fact]
    public void CreateOrder_ThreadSafety()
    {
        var snsEvent = AwsEventBuilder.CreateSnsEvent(CreateOrder, CreateCreateOrderMessage());
        var sqsEvent = AwsEventBuilder.CreateSqsEvent(CreateOrder, CreateCreateOrderMessage());

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

        var orders = GetPersistedOrders();

        Assert.Equal(20, orders.Length);

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(Defaults.Order.Status, orders[i].Status);
            Assert.Equal(Defaults.Order.Name, orders[i].Name);
        }
    }
}