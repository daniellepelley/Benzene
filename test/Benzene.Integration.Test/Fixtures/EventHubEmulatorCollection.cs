using Xunit;

namespace Benzene.Integration.Test.Fixtures;

/// <summary>
/// Shares a single Event Hubs emulator container (AMQP on 5672, Kafka-compatible protocol on 9092)
/// across every test class in this collection, so xunit builds it once and runs those classes
/// sequentially rather than starting a second container that would collide on the same fixed host
/// ports. Both the AMQP-native Event Hub test and the Kafka-protocol test use this same emulator,
/// each against its own entity (<c>eh1</c> vs <c>kafka1</c>) so consumed/produced events don't
/// cross-contaminate between the two.
/// </summary>
[CollectionDefinition(Name)]
public class EventHubEmulatorCollection : ICollectionFixture<EventHubFixture>
{
    public const string Name = "Event Hubs Emulator";
}
