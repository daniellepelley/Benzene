using Azure.Messaging.ServiceBus;
using Benzene.Azure.ServiceBus;
using Benzene.Core.MessageHandlers;
using Benzene.HostedService;
using Benzene.Integration.Test.Fixtures;
using Benzene.Integration.Test.Helpers;
using Benzene.SelfHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Benzene.Integration.Test.ServiceBus;

// Reuses the same Service Bus emulator container as ServiceBusConsumerPipelineTest (see
// DockerEmulatorCollection), but its own queue (benzene-worker-queue, added to
// servicebus-emulator-config.json alongside benzene-queue). Queues are competing-consumer
// entities, so sharing the other test's queue would let this worker steal the message that test
// receives by hand. Unlike ServiceBusConsumerPipelineTest (which exercises
// Benzene.Azure.Function.ServiceBus's trigger-driven application against a hand-received
// message), this test exercises Benzene.Azure.ServiceBus's BenzeneServiceBusWorker end to end:
// a real ServiceBusProcessor consuming the queue and dispatching through the message-handler
// pipeline.
[Collection(DockerEmulatorCollection.Name)]
public class BenzeneServiceBusWorkerLiveTest
{
    private const string ConnectionString =
        "Endpoint=sb://localhost:5673;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
    private const string QueueName = "benzene-worker-queue";

    [Fact]
    public async Task StartAsync_ConsumesRealServiceBusMessage_DispatchesThroughPipeline()
    {
        var mockExampleService = new Mock<IExampleService>();
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        mockExampleService
            .Setup(x => x.Register(It.IsAny<string>()))
            .Callback(() => received.TrySetResult());

        // Sending first (with its own readiness retry) means the emulator is known-reachable
        // before the worker's processor starts, so the worker itself needs no retry loop here.
        await SendAsync();

        var config = new BenzeneServiceBusConfig { QueueName = QueueName };
        await using var client = new ServiceBusClient(ConnectionString);

        var inlineSelfHostedStartUp = new InlineSelfHostedStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
                .AddSingleton(mockExampleService.Object))
            .Configure(app => app.UseServiceBus(config, new ServiceBusClientFactory(client),
                serviceBus => serviceBus.UseMessageHandlers()));

        var host = new HostBuilder()
            .ConfigureServices(services =>
                services.AddHostedService(_ => inlineSelfHostedStartUp.BuildHostedService()))
            .Build();

        await host.StartAsync();
        try
        {
            var completedTask = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(60)));
            Assert.Same(received.Task, completedTask);
        }
        finally
        {
            await host.StopAsync();
        }

        mockExampleService.Verify(x => x.Register(Defaults.Name), Times.AtLeastOnce);
    }

    private static async Task SendAsync()
    {
        // 180s, not 60s: on a cold CI runner this fixture's container may still be settling by
        // the time this test's own retry window starts, since collection-fixture construction for
        // every emulator in DockerEmulatorCollection happens up front, before any test runs.
        var deadline = DateTime.UtcNow.AddSeconds(180);
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            await using var client = new ServiceBusClient(ConnectionString);
            var sender = client.CreateSender(QueueName);
            try
            {
                var message = new ServiceBusMessage(Defaults.Message);
                message.ApplicationProperties["topic"] = Defaults.Topic;

                await sender.SendMessageAsync(message);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new TimeoutException("Timed out waiting for the Service Bus emulator to become ready.", lastException);
    }
}
