namespace Benzene.Integration.Test.Fixtures;

public class ServiceBusFixture : DockerComposeFixture
{
    public ServiceBusFixture()
        : base("ServiceBus/servicebus-docker-compose.yaml")
    { }
}
