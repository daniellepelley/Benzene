namespace Benzene.Integration.Test.Fixtures;

public class EventHubFixture : DockerComposeFixture
{
    public EventHubFixture()
        : base("EventHub/eventhub-docker-compose.yaml")
    { }
}
