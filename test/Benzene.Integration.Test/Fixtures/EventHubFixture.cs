namespace Benzene.Integration.Test.Fixtures;

public class EventHubFixture : DockerComposeFixture
{
    public EventHubFixture()
        : base("eventhub-docker-compose.yaml")
    { }
}
