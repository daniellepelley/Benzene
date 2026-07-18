using Benzene.Clients;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.HostedService;
using Benzene.Integration.Test.Fixtures;
using Benzene.Integration.Test.Helpers;
using Benzene.RabbitMq;
using Benzene.RabbitMq.RabbitMqSendMessage;
using Benzene.Results;
using Benzene.SelfHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace Benzene.Integration.Test.RabbitMq;

// Uses the RabbitMQ container from DockerEmulatorCollection (rabbitmq-docker-compose.yaml). Unlike
// the mocked-channel unit tests (test/Benzene.Core.Test/RabbitMq/RabbitMqWorkerTest.cs), this
// exercises Benzene.RabbitMq end to end against a real broker: a RabbitMqWorker consuming a real
// queue via its AsyncEventingBasicConsumer, dispatching through the message-handler pipeline, and
// acking - and it publishes via the real RabbitMqBenzeneMessageClient, so the outbound path is
// exercised live too. The queue is named after Defaults.Topic ("example") so a default-exchange
// publish (routing key = topic) lands in the queue the worker consumes.
[Collection(DockerEmulatorCollection.Name)]
public class RabbitMqWorkerLiveTest
{
    private const string HostName = "localhost";

    // Remapped to 5674 on the host side (see rabbitmq-docker-compose.yaml) to avoid colliding with
    // the Event Hubs emulator, which claims the broker's default 5672.
    private const int Port = 5674;
    private const string UserName = "benzene";
    private const string Password = "benzene";
    private const string QueueName = Defaults.Topic;

    private static ConnectionFactory CreateConnectionFactory() => new()
    {
        HostName = HostName,
        Port = Port,
        UserName = UserName,
        Password = Password,
    };

    [Fact]
    public async Task StartAsync_ConsumesRealRabbitMqMessage_DispatchesThroughPipeline()
    {
        var mockExampleService = new Mock<IExampleService>();
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        mockExampleService
            .Setup(x => x.Register(It.IsAny<string>()))
            .Callback(() => received.TrySetResult());

        // Declaring the queue also doubles as the broker-readiness wait - it retries until the
        // container's AMQP port is accepting connections.
        await using var connection = await ConnectRetryingAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(QueueName, durable: false, exclusive: false, autoDelete: false);

        var config = new RabbitMqConfig { QueueName = QueueName, ConcurrentRequests = 1 };

        var inlineSelfHostedStartUp = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object))
            .Configure(app => app.UseRabbitMq(config, new RabbitMqConnectionFactory(CreateConnectionFactory()),
                rabbit => rabbit.UseMessageHandlers()));

        var host = new HostBuilder()
            .ConfigureServices(services =>
                services.AddHostedService(_ => inlineSelfHostedStartUp.BuildHostedService()))
            .Build();

        // Start the worker (now consuming the declared queue), then publish - the message is
        // delivered live to an already-listening consumer.
        await host.StartAsync();
        try
        {
            var client = new RabbitMqBenzeneMessageClient(channel,
                NullLogger<RabbitMqBenzeneMessageClient>.Instance, new NullServiceResolver());
            var result = await client.SendMessageAsync<object, object>(Defaults.Topic, Defaults.MessageAsObject);
            Assert.Equal(BenzeneResultStatus.Accepted, result.Status);

            var completedTask = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(120)));
            Assert.Same(received.Task, completedTask);
        }
        finally
        {
            await host.StopAsync();
        }

        mockExampleService.Verify(x => x.Register(Defaults.Name), Times.AtLeastOnce);
    }

    private static async Task<IConnection> ConnectRetryingAsync()
    {
        var factory = CreateConnectionFactory();
        var deadline = DateTime.UtcNow.AddSeconds(180);
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                return await factory.CreateConnectionAsync();
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new TimeoutException("Timed out waiting for the RabbitMQ broker to become ready.", lastException);
    }
}
