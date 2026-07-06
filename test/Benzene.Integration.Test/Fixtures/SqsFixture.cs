namespace Benzene.Integration.Test.Fixtures;

public class SqsFixture : DockerComposeFixture
{
    public SqsFixture()
        : base("sqs-docker-compose.yaml")
    { }
}