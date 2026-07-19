using Amazon.Lambda.APIGatewayEvents;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Benzene.Examples.App.Model.Messages;
using Benzene.Examples.Aws.Dev.Test.Fixtures;
using Benzene.Testing;
using Benzene.Tools.Aws;
using Newtonsoft.Json;
using Xunit;

namespace Benzene.Examples.Aws.Dev.Test;

/// <summary>
/// The real-dependency counterpart of the in-memory <c>PublishOrderCreatedTest</c>: it runs the AWS
/// example's SQS <b>egress</b> demo (<c>POST /orders/publish-created</c> →
/// <c>PublishOrderCreatedMessageHandler</c> → the real <c>.UseSqs(MY_QUEUE_URL)</c> outbound route)
/// against a real LocalStack SQS queue, then drains the queue with the AWS SDK to prove a message
/// actually landed on the wire with the right topic and payload - not a fake sender.
///
/// This tier needs Docker (LocalStack), so this project is deliberately kept out of
/// Benzene.Examples.sln and run by its own CI job, following the .Dev.Test convention.
/// </summary>
[Collection("Sequential")]
public class PublishOrderCreatedLocalStackTest : IClassFixture<LocalStackFixture>
{
    private const string ServiceUrl = "http://localhost:4566";
    private const string AccessKey = "123";
    private const string SecretKey = "xyz";
    private const string QueueName = "order-created-queue";

    private static IAmazonSQS CreateSqsClient() =>
        new AmazonSQSClient(new BasicAWSCredentials(AccessKey, SecretKey), new AmazonSQSConfig { ServiceURL = ServiceUrl });

    [Fact]
    public async Task PublishOrderCreated_ActuallySendsToTheRealSqsQueue()
    {
        // 1. Create the real queue in LocalStack and capture its URL.
        var sqs = CreateSqsClient();
        var queueUrl = (await sqs.CreateQueueAsync(new CreateQueueRequest(QueueName))).QueueUrl;

        // 2. Point the example at LocalStack BEFORE building the host - DependenciesBuilder reads
        //    these at ConfigureServices time (the SDK's default credential chain picks up the
        //    AWS_ACCESS_KEY_ID/SECRET env vars, and the outbound route sends to MY_QUEUE_URL).
        Environment.SetEnvironmentVariable("AWS_SERVICE_URL", ServiceUrl);
        Environment.SetEnvironmentVariable("AWS_DEFAULT_REGION", "eu-central-1");
        Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", AccessKey);
        Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", SecretKey);
        Environment.SetEnvironmentVariable("MY_QUEUE_URL", queueUrl);

        using var host = new AwsLambdaBenzeneTestHost(BenzeneTestHost.Create<StartUp>().BuildAwsLambdaHost());

        // 3. Drive the egress handler over the API Gateway HTTP surface, exactly as deployed.
        var orderCreated = new OrderCreatedEvent { Id = Guid.NewGuid(), Name = "acme" };
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/orders/publish-created",
            Body = JsonConvert.SerializeObject(orderCreated),
            Headers = new Dictionary<string, string> { { "x-correlation-id", Guid.NewGuid().ToString() } }
        };
        var response = await host.SendEventAsync<APIGatewayProxyResponse>(request);
        // A successful real SQS send maps its HTTP 200 to BenzeneResultStatus.Ok (unlike the
        // in-memory PublishOrderCreatedTest, whose fake sender returns Accepted/202) - so assert a
        // 2xx here and let the drained message below carry the real proof.
        Assert.InRange(response.StatusCode, 200, 299);

        // 4. Drain the real queue and assert the message actually arrived on the wire.
        var messages = await ReceiveAllAsync(sqs, queueUrl);
        var message = Assert.Single(messages);
        Assert.Equal("order_created", message.MessageAttributes["topic"].StringValue);
        var delivered = JsonConvert.DeserializeObject<OrderCreatedEvent>(message.Body);
        Assert.Equal(orderCreated.Id, delivered!.Id);
        Assert.Equal("acme", delivered.Name);
    }

    private static async Task<List<Message>> ReceiveAllAsync(IAmazonSQS sqs, string queueUrl)
    {
        var all = new List<Message>();
        // LocalStack is local and the send completes before the handler returns, but SQS receive is
        // still eventually-consistent - poll briefly (long-poll) until a message shows up.
        for (var attempt = 0; attempt < 5 && all.Count == 0; attempt++)
        {
            var result = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MessageAttributeNames = new List<string> { "All" },
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 2
            });
            all.AddRange(result.Messages);
        }
        return all;
    }
}
