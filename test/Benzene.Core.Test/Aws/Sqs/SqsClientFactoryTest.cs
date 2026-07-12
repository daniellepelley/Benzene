using Amazon.SQS;
using Benzene.Aws.Sqs.Consumer;
using Moq;
using Xunit;

namespace Benzene.Test.Aws.Sqs;

public class SqsClientFactoryTest
{
    [Fact]
    public void Create_ReturnsInjectedClient()
    {
        var mockAmazonSqs = new Mock<IAmazonSQS>();
        var factory = new SqsClientFactory(mockAmazonSqs.Object);

        var result = factory.Create();

        Assert.Same(mockAmazonSqs.Object, result);
    }
}
