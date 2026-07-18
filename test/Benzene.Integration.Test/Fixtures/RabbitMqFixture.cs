namespace Benzene.Integration.Test.Fixtures;

public class RabbitMqFixture : DockerComposeFixture
{
    public RabbitMqFixture()
        : base("RabbitMq/rabbitmq-docker-compose.yaml")
    { }
}
