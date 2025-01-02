namespace Benzene.Aws.Tests.Fixtures;

public class SqsFixture : LocalStackFixture
{
    public SqsFixture()
        : base("sqs-docker-compose.yaml")
    { }
}