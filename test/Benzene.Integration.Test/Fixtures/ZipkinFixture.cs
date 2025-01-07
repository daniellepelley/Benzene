namespace Benzene.Integration.Test.Fixtures;

public class ZipkinFixture : DockerComposeFixture
{
    public ZipkinFixture()
        : base("zipkin-docker-compose.yaml")
    { }
}