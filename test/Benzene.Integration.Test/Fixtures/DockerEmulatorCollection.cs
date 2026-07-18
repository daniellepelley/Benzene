using Xunit;

namespace Benzene.Integration.Test.Fixtures;

/// <summary>
/// Shares one xunit collection across every Docker Compose-based emulator fixture
/// (<see cref="SqsFixture"/>, <see cref="EventHubFixture"/>, <see cref="ServiceBusFixture"/>), so
/// xunit builds all three sequentially and runs every test in this collection sequentially too -
/// on a resource-constrained CI runner, starting up to 5 heavy container images (localstack,
/// azurite, eventhubs-emulator, servicebus-emulator, mssql) concurrently caused some of them to
/// time out waiting for their emulator to become reachable. The Event Hub and Kafka tests further
/// share the one <see cref="EventHubFixture"/> instance between them (it exposes both the native
/// AMQP endpoint and a Kafka-compatible endpoint on the same emulator container) via separate
/// entities (<c>eh1</c> vs <c>kafka1</c>) so their events don't cross-contaminate.
/// </summary>
[CollectionDefinition(Name)]
public class DockerEmulatorCollection :
    ICollectionFixture<SqsFixture>,
    ICollectionFixture<EventHubFixture>,
    ICollectionFixture<ServiceBusFixture>,
    ICollectionFixture<RabbitMqFixture>
{
    public const string Name = "Docker Emulators";
}
