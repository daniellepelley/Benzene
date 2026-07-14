namespace Benzene.Integration.Test.Fixtures;

public class ServiceBusFixture : DockerComposeFixture
{
    public ServiceBusFixture()
        : base("servicebus-docker-compose.yaml")
    { }
}
