using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Benzene.Examples.App.Data;
using Benzene.Examples.Aws.Tests.Helpers;
using Benzene.Examples.Aws.Tests.Helpers.Builders;
using Benzene.Tools.Aws;
using Xunit;
using ThreadSafeTestLambdaLogger = Benzene.Examples.Aws.Tests.Helpers.ThreadSafeTestLambdaLogger;

namespace Benzene.Examples.Aws.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Sequential")]
public class HealthCheckTest : InMemoryOrdersTestBase
{
    private const string HealthCheckTopic = "healthcheck";
    private readonly Guid _id = Guid.Parse(Defaults.Order.Id);

    public HealthCheckTest()
        :base(new AwsLambdaBenzeneTestStartUp<StartUp>().Build())
    { }
    
    [Fact]
    public async Task HealthCheck_SNSEvent()
    {
        AddOrder(new Order
        {
            Id = _id,
            Status = Defaults.Order.Status,
        });

        var snsEvent = AwsEventBuilder.CreateSnsEvent(HealthCheckTopic, null);

        await TestLambdaHosting.SendEventAsync(snsEvent);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{GetOrder}:result", messages[0].GetTopic());
        // Assert.Equal("200", messages[0].GetStatus());
    }

    [Fact]
    public async Task HealthCheck_SQSEvent()
    {
        var sqsEvent = AwsEventBuilder.CreateSqsEvent(HealthCheckTopic, null);

        await TestLambdaHosting.SendEventAsync(sqsEvent);

        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{GetOrder}:result", messages[0].GetTopic());
        // Assert.Equal("200", messages[0].GetStatus());
    }


    [Fact]
    public async Task HealthCheck_ApiGateway()
    {
        var apiGatewayProxyRequest = new ApiGatewayProxyRequestBuilder("POST", $"/healthcheck")
            .Build();

        var response = await TestLambdaHosting.SendEventAsync<APIGatewayProxyResponse>(apiGatewayProxyRequest);
            
        Assert.Equal(200, response.StatusCode);

    }

    [Fact]
    public async Task GetOrder_ThreadSafety()
    {
        AddOrder(new Order
        {
            Id = _id,
            Status = Defaults.Order.Status,
        });
    
        var snsEvent = AwsEventBuilder.CreateSnsEvent(HealthCheckTopic, null);
        var sqsEvent = AwsEventBuilder.CreateSqsEvent(HealthCheckTopic, null);
    
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
    
        // await Task.Delay(2000);
    
        // var messages = await SqsSetUp.GetAllMessagesAsync();
        // Assert.Equal($"{GetOrder}:result", messages[0].GetTopic());
        // Assert.Equal(20, messages.Length);
    }
}