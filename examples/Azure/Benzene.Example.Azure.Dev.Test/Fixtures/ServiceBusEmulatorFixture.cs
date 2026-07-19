using Azure.Messaging.ServiceBus;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;

namespace Benzene.Example.Azure.Dev.Test.Fixtures;

/// <summary>
/// Starts the Azure Service Bus emulator (plus its required MSSQL backend) via
/// <c>Ductus.FluentDocker</c> and blocks until it actually serves the <c>orders</c> queue the Azure
/// example publishes to. Mirrors the library-level <c>test/Benzene.Integration.Test</c>
/// <c>ServiceBusFixture</c> + <c>servicebus-docker-compose.yaml</c> - kept as a self-contained copy
/// so the example tier doesn't reach into the library test project.
///
/// Unlike LocalStack, the Service Bus emulator exposes no HTTP health endpoint, so readiness is
/// proven the only way that actually matters here: by successfully sending a message to the queue.
/// The retry window is generous (matching the library's <c>BenzeneServiceBusWorkerLiveTest</c>)
/// because on a cold CI runner the MSSQL backend the emulator depends on can still be starting up
/// well after the containers report "created".
/// </summary>
public class ServiceBusEmulatorFixture : IDisposable
{
    public const string ConnectionString =
        "Endpoint=sb://localhost:5673;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
    public const string QueueName = "orders";

    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(180);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly ICompositeService _compositeService;

    public ServiceBusEmulatorFixture()
    {
        _compositeService = StartEmulator("Fixtures/Files/servicebus-docker-compose.yaml");
        WaitUntilReady();
    }

    public void Dispose()
    {
        _compositeService.Dispose();
        GC.SuppressFinalize(this);
    }

    private static ICompositeService StartEmulator(string fileName)
    {
        return new Builder()
            .UseContainer()
            .UseCompose()
            .FromFile(Path.Combine(Directory.GetCurrentDirectory(), (TemplateString)fileName))
            .ForceBuild()
            .Build()
            .Start();
    }

    private static void WaitUntilReady()
    {
        var deadline = DateTime.UtcNow + ReadyTimeout;
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                // A send that succeeds is proof the broker is up AND the "orders" queue exists -
                // exactly the two things the example's egress route needs. The probe is drained
                // immediately so it can't be mistaken for the message the test itself publishes.
                SendProbeAndDrainAsync().GetAwaiter().GetResult();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Thread.Sleep(PollInterval);
            }
        }

        throw new TimeoutException(
            $"Service Bus emulator did not serve queue '{QueueName}' within {ReadyTimeout}.", lastException);
    }

    private static async Task SendProbeAndDrainAsync()
    {
        await using var client = new ServiceBusClient(ConnectionString);

        var sender = client.CreateSender(QueueName);
        await sender.SendMessageAsync(new ServiceBusMessage("readiness-probe"));

        var receiver = client.CreateReceiver(QueueName);
        while (true)
        {
            var batch = await receiver.ReceiveMessagesAsync(maxMessages: 10, maxWaitTime: TimeSpan.FromSeconds(2));
            if (batch.Count == 0)
            {
                return;
            }

            foreach (var message in batch)
            {
                await receiver.CompleteMessageAsync(message);
            }
        }
    }
}
