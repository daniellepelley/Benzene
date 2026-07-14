namespace Benzene.Integration.Test.Fixtures;

public class SqsFixture : DockerComposeFixture
{
    public SqsFixture()
        : base("Sqs/sqs-docker-compose.yaml")
    { }
}