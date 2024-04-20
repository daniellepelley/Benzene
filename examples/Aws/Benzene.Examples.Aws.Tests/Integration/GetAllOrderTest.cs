using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Benzene.Examples.App.Data;
using Benzene.Examples.App.Handlers;
using Benzene.Examples.App.Model;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Aws.Tests.Helpers;
using Benzene.Examples.Aws.Tests.Helpers.Builders;
using Xunit;

namespace Benzene.Examples.Aws.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Sequential")]
public class GetAllOrderTest : InMemoryOrdersTestBase
{
    private const string GetAllOrders = MessageTopicNames.OrderGetAll;
    private readonly Guid _id1 = Guid.Parse(Defaults.Order.Id);
    private readonly Guid _id2 = Guid.Parse(Defaults.Order.Id2);

    public GetAllOrderTest()
        :base(new TestLambdaStartUp<StartUp>().Build())
    { }
    
    private static GetAllOrdersMessage CreateGetAllOrdersMessage()
    {
        return new GetAllOrdersMessage();
    }

    [Fact]
    public async Task GetAllOrders_SNSEvent()
    {
        SetUpDatabase();

        var snsEvent = AwsEventBuilder.CreateSnsEvent(GetAllOrders, CreateGetAllOrdersMessage());

        await TestLambdaHosting.SendEventAsync(snsEvent);
            
        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{GetAllOrders}:result", messages[0].GetTopic());
        // Assert.True(messages[0].BodyContains(_id1.ToString()));
        // Assert.True(messages[0].BodyContains(_id2.ToString()));
    }

    [Fact]
    public async Task GetAllOrders_SQSEvent()
    {
        SetUpDatabase();

        var sqsEvent = AwsEventBuilder.CreateSqsEvent(GetAllOrders, CreateGetAllOrdersMessage());

        await TestLambdaHosting.SendEventAsync(sqsEvent);
            
        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{GetAllOrders}:result", messages[0].GetTopic());
        // Assert.True(messages[0].BodyContains(_id1.ToString()));
        // Assert.True(messages[0].BodyContains(_id2.ToString()));
    }


    [Fact]
    public async Task GetAllOrders_ApiGateway()
    {
        SetUpDatabase();
            
        var apiGatewayProxyRequest = new ApiGatewayProxyRequestBuilder("GET", "/orders")
            .WithBody(CreateGetAllOrdersMessage())
            .Build();

        for (int i = 0; i < 5; i++)
        {
            await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);
        }
        
        var response = await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);

        var orders = response.Body<OrderDto[]>();

        Assert.True(orders.Any(x => x.Id == _id1));
        Assert.True(orders.Any(x => x.Id == _id2));
    }

    [Fact]
    public async Task GetAllOrders_WhenNoOrdersExist()
    {
        var sqsEvent = AwsEventBuilder.CreateSqsEvent(GetAllOrders, CreateGetAllOrdersMessage());

        await TestLambdaHosting.SendEventAsync(sqsEvent);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{GetAllOrders}:result", messages[0].GetTopic());
    }

    [Fact]
    public async Task PostAllOrders_ThreadSafety()
    {
        AddOrder(new Order
        {
            Id = _id1,
            Status = Defaults.Order.Status,
            Name = Defaults.Order.Name,
        });
        AddOrder(new Order
        {
            Id = _id2,
            Status = Defaults.Order.Status2,
            Name = Defaults.Order.Name2,
        });

        var snsEvent = AwsEventBuilder.CreateSnsEvent(GetAllOrders, CreateGetAllOrdersMessage());
        var sqsEvent = AwsEventBuilder.CreateSqsEvent(GetAllOrders, CreateGetAllOrdersMessage());

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
        // Assert.Equal($"{GetAllOrders}:result", messages[0].GetTopic());
        // Assert.Equal(20, messages.Length);
    }

    private void SetUpDatabase()
    {
        AddOrder(new Order
        {
            Id = _id1,
            Status = Defaults.Order.Status,
            Name = Defaults.Order.Name,
        });
        AddOrder(new Order
        {
            Id = _id2,
            Status = Defaults.Order.Status2,
            Name = Defaults.Order.Name2,
        });
    }
}